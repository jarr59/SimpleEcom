// Valida la Api, la tienda (HTML que ve un buscador + navegador) y el Admin (CRUD real en navegador)
// contra instancias ya arrancadas. Lo lanza run.ps1; también se puede ejecutar a mano con `node validate.mjs`.
//
// Datos de prueba: todo lo que crea lleva el prefijo QAVAL y se borra al terminar (también los restos de
// ejecuciones anteriores). Nunca toca datos sin ese prefijo.
//
// Salida: una línea PASS/WARN/FAIL por comprobación, report.json y capturas de los fallos en OUT.
// Código de salida 1 si algo falla.
import { chromium, request } from 'playwright';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const API = process.env.SIMPLEECOM_API ?? 'https://localhost:7221/';
const STORE = process.env.SIMPLEECOM_STORE ?? 'https://localhost:7171/';
const ADMIN = process.env.SIMPLEECOM_ADMIN ?? 'https://localhost:7146/';
const OUT = process.env.SIMPLEECOM_VALIDATION_OUT ?? join(process.cwd(), 'out');
const WAIT_SECONDS = Number(process.env.SIMPLEECOM_WAIT_SECONDS ?? 120);
const PREFIX = 'QAVAL';
const RUN = `${PREFIX}${Date.now().toString(36).toUpperCase()}`;
const MISSING_ID = '00000000-0000-0000-0000-000000000001';

mkdirSync(OUT, { recursive: true });

// ---------- Resultados ----------

const results = [];

class Warning extends Error {}

/** Ejecuta una comprobación. Lanza Error = FAIL, Warning = WARN; si devuelve texto, es el detalle del PASS. */
async function check(area, name, fn, page) {
  try {
    const detail = await fn();
    record(area, name, 'pass', detail);
  } catch (error) {
    const status = error instanceof Warning ? 'warn' : 'fail';
    let detail = error.message.split('\n')[0];
    if (status === 'fail' && page && !page.isClosed()) {
      const file = join(OUT, `${area}-${name}`.replace(/[^\w-]+/g, '_') + '.png');
      try {
        await page.screenshot({ path: file, fullPage: true });
        detail += ` (captura: ${file})`;
      } catch { /* sin captura */ }
    }
    record(area, name, status, detail);
  }
}

function record(area, name, status, detail) {
  results.push({ area, name, status, detail: detail ?? null });
  console.log(`${status.toUpperCase().padEnd(4)} ${area.padEnd(6)} ${name}${detail ? ` — ${detail}` : ''}`);
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function warnIf(condition, message) {
  if (condition) throw new Warning(message);
}

// ---------- HTML ----------

function decode(text) {
  return text
    .replace(/&#x([0-9a-f]+);/gi, (_, hex) => String.fromCodePoint(parseInt(hex, 16)))
    .replace(/&#(\d+);/g, (_, dec) => String.fromCodePoint(Number(dec)))
    .replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
}

/** Lo que un buscador ve sin ejecutar JavaScript: lo que llega antes de los bloques de streaming. */
function initialHtml(html) {
  const i = html.indexOf('<blazor-ssr');
  return decode(i < 0 ? html : html.slice(0, i));
}

const pick = (html, regex) => html.match(regex)?.[1] ?? null;

function seo(html) {
  return {
    title: pick(html, /<title>([\s\S]*?)<\/title>/),
    description: pick(html, /<meta name="description" content="([^"]*)"/),
    canonical: pick(html, /<link rel="canonical" href="([^"]*)"/),
    robots: pick(html, /<meta name="robots" content="([^"]*)"/),
    jsonLd: pick(html, /<script type="application\/ld\+json">([\s\S]*?)<\/script>/),
  };
}

// ---------- Http ----------

const http = await request.newContext({ ignoreHTTPSErrors: true, timeout: 30_000 });

async function get(url) {
  const response = await http.get(url, { maxRedirects: 0, failOnStatusCode: false });
  return { status: response.status(), text: await response.text(), headers: response.headers() };
}

async function apiJson(path) {
  const r = await get(API + path);
  assert(r.status === 200, `GET ${path} devolvió ${r.status}`);
  return JSON.parse(r.text);
}

async function waitForApps() {
  const deadline = Date.now() + WAIT_SECONDS * 1000;
  for (const [name, url] of [['api', API + 'api/branches'], ['tienda', STORE + 'robots.txt'], ['admin', ADMIN]]) {
    for (;;) {
      try {
        if ((await http.get(url, { failOnStatusCode: false, timeout: 5_000 })).status() < 500) break;
      } catch { /* aún arrancando */ }
      if (Date.now() > deadline) throw new Error(`${name} no responde en ${url} tras ${WAIT_SECONDS} s`);
      await new Promise(r => setTimeout(r, 1_000));
    }
  }
}

// ---------- Datos de prueba ----------

async function cleanup() {
  const removed = [];
  for (const [kind, key] of [['products', 'sku'], ['branches', 'code']]) {
    let items;
    try {
      items = await apiJson(`api/${kind}?search=${PREFIX}`);
    } catch {
      continue;
    }
    for (const item of items.filter(i => i[key]?.toUpperCase().startsWith(PREFIX))) {
      const r = await http.delete(`${API}api/${kind}/${item.id}`, { failOnStatusCode: false });
      removed.push(`${item[key]} (${r.status()})`);
    }
  }
  return removed;
}

// ---------- Comprobaciones ----------

let products = [];
let branches = [];
let activeProduct = null;

async function checkApi() {
  await check('api', 'listado de productos', async () => {
    products = await apiJson('api/products');
    assert(Array.isArray(products), 'la respuesta no es una lista');
    activeProduct = products.find(p => p.isActive) ?? null;
    warnIf(!activeProduct, 'no hay productos activos: se omiten las pruebas de ficha');
    return `${products.length} productos, ${products.filter(p => p.isActive).length} activos`;
  });
  await check('api', 'listado de sucursales', async () => {
    branches = await apiJson('api/branches');
    assert(Array.isArray(branches), 'la respuesta no es una lista');
    warnIf(!branches.some(b => b.isActive), 'no hay sucursales activas');
    return `${branches.length} sucursales, ${branches.filter(b => b.isActive).length} activas`;
  });
  await check('api', 'producto inexistente → 404', async () => {
    const r = await get(`${API}api/products/${MISSING_ID}`);
    assert(r.status === 404, `devolvió ${r.status}`);
  });
}

/** Comprobaciones comunes de SEO en una página pública de la tienda. */
async function storePage(path, { status = 200, noindex = false, needles = [] } = {}) {
  const r = await get(STORE + path);
  assert(r.status === status, `HTTP ${r.status}, se esperaba ${status}`);
  const html = initialHtml(r.text);
  const meta = seo(html);
  assert(meta.title?.trim(), 'sin <title>');
  assert(meta.description?.trim(), 'sin meta description');
  assert(meta.canonical?.startsWith(STORE), `canonical no absoluto: ${meta.canonical}`);
  const hasNoindex = meta.robots?.includes('noindex') ?? false;
  assert(hasNoindex === noindex, noindex ? 'falta noindex' : `noindex inesperado (${meta.robots})`);
  for (const needle of needles) {
    assert(html.includes(needle), `"${needle}" no está en el HTML inicial (¿streaming o fallo de la Api?)`);
  }
  return { html, meta };
}

async function checkStoreHtml() {
  const activeBranches = branches.filter(b => b.isActive);

  await check('tienda', 'portada', async () => {
    const { html, meta } = await storePage('', { needles: activeProduct ? ['product-card__name'] : [] });
    return `${(html.match(/product-card__name/g) ?? []).length} tarjetas · "${meta.title}"`;
  });
  await check('tienda', 'catálogo', async () => {
    const { html } = await storePage('products', { needles: activeProduct ? [activeProduct.name] : [] });
    const inactive = products.filter(p => !p.isActive && html.includes(`products/${p.id}`));
    assert(inactive.length === 0, `muestra productos inactivos: ${inactive.map(p => p.sku).join(', ')}`);
    return `${(html.match(/product-card__name/g) ?? []).length} tarjetas`;
  });
  await check('tienda', 'búsqueda con noindex', async () => {
    await storePage(`products?q=${encodeURIComponent(activeProduct?.name.split(' ')[0] ?? 'x')}`, { noindex: true });
  });
  if (activeProduct) {
    await check('tienda', 'ficha de producto', async () => {
      const { meta } = await storePage(`products/${activeProduct.id}`, { needles: [activeProduct.name, activeProduct.sku] });
      assert(meta.canonical === `${STORE}products/${activeProduct.id}`, `canonical ${meta.canonical}`);
      assert(meta.jsonLd, 'sin JSON-LD');
      const ld = JSON.parse(meta.jsonLd);
      assert(ld['@type'] === 'Product', `@type ${ld['@type']}`);
      assert(Number(ld.offers?.price) === Number(activeProduct.price), `precio JSON-LD ${ld.offers?.price} ≠ Api ${activeProduct.price}`);
      const availability = activeProduct.stock > 0 ? 'InStock' : 'OutOfStock';
      assert(ld.offers?.availability?.endsWith(availability), `disponibilidad ${ld.offers?.availability}, se esperaba ${availability}`);
      return `${ld.name} · ${ld.offers.price} ${ld.offers.priceCurrency} · ${availability}`;
    });
  }
  await check('tienda', 'producto inexistente → 404', async () => {
    await storePage(`products/${MISSING_ID}`, { status: 404, noindex: true, needles: ['no está disponible'] });
  });
  await check('tienda', 'sucursales', async () => {
    await storePage('branches', { needles: activeBranches.map(b => b.name) });
    return `${activeBranches.length} sucursales activas en el HTML`;
  });
  await check('tienda', 'ruta inexistente → 404', async () => {
    const r = await get(`${STORE}${PREFIX.toLowerCase()}-no-existe`);
    assert(r.status === 404, `HTTP ${r.status}`);
  });
  await check('tienda', 'robots.txt', async () => {
    const r = await get(STORE + 'robots.txt');
    assert(r.status === 200, `HTTP ${r.status}`);
    assert(r.text.includes(`Sitemap: ${STORE}sitemap.xml`), 'sin línea Sitemap');
    assert(r.text.includes('Disallow: /branches/select'), 'no bloquea /branches/select');
  });
  await check('tienda', 'sitemap.xml', async () => {
    const r = await get(STORE + 'sitemap.xml');
    assert(r.status === 200, `HTTP ${r.status}`);
    const urls = [...r.text.matchAll(/<loc>([^<]*)<\/loc>/g)].map(m => decode(m[1]));
    for (const path of ['', 'products', 'branches']) assert(urls.includes(STORE + path), `falta ${STORE + path}`);
    const missing = products.filter(p => p.isActive && !urls.includes(`${STORE}products/${p.id}`));
    const extra = products.filter(p => !p.isActive && urls.includes(`${STORE}products/${p.id}`));
    assert(missing.length === 0, `faltan productos activos: ${missing.map(p => p.sku).join(', ')}`);
    assert(extra.length === 0, `incluye productos inactivos: ${extra.map(p => p.sku).join(', ')}`);
    return `${urls.length} URLs`;
  });
}

/** Abre una página y registra errores de consola/JS y el aviso de error de Blazor. */
async function openPage(context, url) {
  const page = await context.newPage();
  page.jsErrors = [];
  page.on('console', m => { if (m.type() === 'error') page.jsErrors.push(m.text()); });
  page.on('pageerror', e => page.jsErrors.push(e.message));
  page.setDefaultTimeout(15_000);
  await page.goto(url, { waitUntil: 'load' });
  return page;
}

async function assertHealthy(page) {
  assert(!(await page.locator('#blazor-error-ui').isVisible()), 'se muestra el aviso de error de Blazor');
  assert(page.jsErrors.length === 0, `errores en consola: ${page.jsErrors.slice(0, 2).join(' | ')}`);
}

async function checkStoreBrowser(browser) {
  const mobile = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 375, height: 800 }, isMobile: true });
  const paths = ['', 'products', 'branches', ...(activeProduct ? [`products/${activeProduct.id}`] : [])];
  for (const path of paths) {
    let page;
    await check('tienda', `móvil 375px /${path}`, async () => {
      page = await openPage(mobile, STORE + path);
      await page.waitForLoadState('networkidle');
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      assert(overflow <= 0, `scroll horizontal de ${overflow}px`);
      await assertHealthy(page);
    }, page);
    await page?.close();
  }
  await mobile.close();

  // Elegir sucursal: formulario POST sin JavaScript → cookie → vuelve a la página de origen con la barra actualizada.
  const desktop = await browser.newContext({ ignoreHTTPSErrors: true });
  let page;
  await check('tienda', 'elegir sucursal', async () => {
    page = await openPage(desktop, `${STORE}branches?returnUrl=${encodeURIComponent('/products')}`);
    const card = page.locator('article.branch-card').filter({ has: page.getByRole('button', { name: /Comprar en esta sucursal/ }) }).first();
    if ((await card.count()) === 0) {
      warnIf(true, 'no hay ninguna sucursal que elegir (¿una sola sucursal ya seleccionada?)');
    }
    const name = (await card.locator('h2').innerText()).trim();
    await card.getByRole('button', { name: /Comprar en esta sucursal/ }).click();
    await page.waitForURL(url => new URL(url).pathname === '/products');
    await page.locator('.branch-bar__text', { hasText: 'Comprando en' }).waitFor();
    const bar = await page.locator('.branch-bar__text').innerText();
    assert(bar.includes(name), `la barra dice "${bar.trim()}", se esperaba ${name}`);
    await assertHealthy(page);
    return name;
  }, page);
  await desktop.close();
}

async function checkAdmin(browser) {
  const context = await browser.newContext({ ignoreHTTPSErrors: true });

  for (const [path, heading] of [['', null], ['products', 'Productos'], ['branches', 'Sucursales']]) {
    let page;
    await check('admin', `carga /${path}`, async () => {
      page = await openPage(context, ADMIN + path);
      if (heading) {
        await page.getByRole('heading', { name: heading, level: 1 }).waitFor();
        await page.locator('table, .border.rounded').first().waitFor();
      }
      await assertHealthy(page);
    }, page);
    await page?.close();
  }

  await adminCrud(context, {
    kind: 'products', label: 'producto', createText: 'Crear producto',
    created: 'Producto creado correctamente.', updated: 'Producto actualizado correctamente.',
    key: 'sku', keyField: '#sku',
    fill: async (page, name) => {
      await setField(page, '#name', name);
      await setField(page, '#description', 'Producto creado por la validación automática.');
      await setField(page, '#price', '12.34');
      await page.locator('#currency').selectOption('USD');
      await setField(page, '#stock', '5');
    },
    edit: async page => setField(page, '#price', '15.5'),
    afterEdit: async item => {
      assert(Number(item.price) === 15.5, `precio en la Api ${item.price}, se esperaba 15.5`);
      const { meta } = await storePage(`products/${item.id}`, { needles: [item.name] });
      assert(Number(JSON.parse(meta.jsonLd).offers.price) === 15.5, 'la tienda no muestra el precio editado');
    },
    afterDeactivate: async item => {
      await storePage(`products/${item.id}`, { status: 404, noindex: true });
      const r = await get(STORE + 'products');
      assert(!r.text.includes(`products/${item.id}`), 'el catálogo sigue mostrando el producto inactivo');
    },
    storeAfterCreate: async item => storePage(`products/${item.id}`, { needles: [item.name] }),
  });

  await adminCrud(context, {
    kind: 'branches', label: 'sucursal', createText: 'Crear sucursal',
    created: 'Sucursal creada correctamente.', updated: 'Sucursal actualizada correctamente.',
    key: 'code', keyField: '#code',
    fill: async (page, name) => {
      await setField(page, '#name', name);
      await setField(page, '#address', 'Calle de la Validación 1');
      await setField(page, '#phone', '+1 555 0100');
      await page.locator('#allowsPickup').check();
    },
    edit: async page => page.locator('#allowsDelivery').check(),
    afterEdit: async item => assert(item.allowsDelivery, 'la Api no guardó el envío a domicilio'),
    afterDeactivate: async item => {
      const r = await get(STORE + 'branches');
      assert(!initialHtml(r.text).includes(item.name), 'la tienda sigue ofreciendo la sucursal inactiva');
    },
    storeAfterCreate: async item => storePage('branches', { needles: [item.name] }),
  });

  await context.close();
}

/** Rellena un campo y sale de él: los Input* de Blazor enlazan en el evento change. */
async function setField(page, selector, value) {
  const field = page.locator(selector);
  await field.fill(value);
  await field.press('Tab');
}

async function adminCrud(context, o) {
  const name = `${RUN} ${o.label} de validación`;
  const editedName = `${name} (editado)`;
  const page = await openPage(context, `${ADMIN}${o.kind}/new`);
  const area = 'admin';
  let item = null;

  const findInApi = async () =>
    (await apiJson(`api/${o.kind}?search=${RUN}`)).find(i => i[o.key].toUpperCase() === RUN) ?? null;

  const row = async () => {
    await page.locator('#search').fill(RUN);
    await page.locator('#search').press('Enter');
    const r = page.locator('tbody tr', { hasText: RUN });
    await r.waitFor();
    return r;
  };

  await check(area, `${o.label}: validación del formulario vacío`, async () => {
    await page.locator(o.keyField).waitFor();
    await page.getByRole('button', { name: o.createText }).click();
    // ValidationMessage recibe class="text-danger", que sustituye a .validation-message: se busca por texto.
    const messages = page.getByText(/es obligatori[oa]\./);
    await messages.first().waitFor();
    assert(page.url().endsWith(`${o.kind}/new`), 'navegó con el formulario vacío');
    return `${await messages.count()} campos obligatorios señalados`;
  }, page);

  await check(area, `${o.label}: crear`, async () => {
    await setField(page, o.keyField, RUN);
    await o.fill(page, name);
    await page.getByRole('button', { name: o.createText }).click();
    await page.waitForURL(/status=created/);
    await page.getByText(o.created).waitFor();
    await row();
    item = await findInApi();
    assert(item, 'no aparece en la Api');
    await o.storeAfterCreate(item);
    await assertHealthy(page);
    return `${item[o.key]} · visible en la tienda`;
  }, page);
  if (!item) return page.close();

  await check(area, `${o.label}: editar`, async () => {
    await (await row()).getByRole('link', { name: 'Editar' }).click();
    await page.waitForURL(/\/edit$/);
    await page.waitForFunction(() => document.querySelector('#name')?.value.length > 0);
    await setField(page, '#name', editedName);
    await o.edit(page);
    await page.getByRole('button', { name: 'Guardar cambios' }).click();
    await page.waitForURL(/status=updated/);
    await page.getByText(o.updated).waitFor();
    item = await findInApi();
    assert(item.name === editedName, `nombre en la Api "${item.name}"`);
    await o.afterEdit(item);
  }, page);

  await check(area, `${o.label}: desactivar`, async () => {
    await (await row()).getByRole('button', { name: 'Desactivar' }).click();
    await page.getByText(/desactivad[oa]\./).waitFor();
    item = await findInApi();
    assert(!item.isActive, 'sigue activo en la Api');
    await o.afterDeactivate(item);
  }, page);

  await check(area, `${o.label}: eliminar`, async () => {
    await (await row()).getByRole('button', { name: 'Eliminar' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.waitFor();
    await dialog.getByRole('button', { name: 'Eliminar', exact: true }).click();
    await page.getByText(/eliminad[oa]\./).waitFor();
    const r = await get(`${API}api/${o.kind}/${item.id}`);
    assert(r.status === 404, `la Api devuelve ${r.status} tras eliminar`);
    await assertHealthy(page);
  }, page);

  await page.close();
}

// ---------- Ejecución ----------

let browser;
try {
  await check('setup', 'aplicaciones arrancadas', async () => { await waitForApps(); });
  if (results.every(r => r.status !== 'fail')) {
    await check('setup', 'limpieza de restos QAVAL', async () => {
      const removed = await cleanup();
      return removed.length ? `borrados: ${removed.join(', ')}` : 'nada que limpiar';
    });
    await checkApi();
    await checkStoreHtml();
    await check('setup', 'Edge (Playwright)', async () => {
      browser = await chromium.launch({ channel: 'msedge', headless: true });
    });
    if (browser) {
      await checkStoreBrowser(browser);
      await checkAdmin(browser);
    }
  }
} catch (error) {
  record('setup', 'error inesperado', 'fail', error.stack ?? String(error));
} finally {
  await browser?.close();
  try {
    const removed = await cleanup();
    if (removed.length) record('setup', 'limpieza final', 'warn', `quedaron datos de prueba y se borraron: ${removed.join(', ')}`);
  } catch { /* la Api puede estar caída */ }
  await http.dispose();
}

const count = s => results.filter(r => r.status === s).length;
const summary = { run: RUN, pass: count('pass'), warn: count('warn'), fail: count('fail'), results };
writeFileSync(join(OUT, 'report.json'), JSON.stringify(summary, null, 2));
console.log(`\nResultado: ${summary.pass} ok · ${summary.warn} avisos · ${summary.fail} fallos — ${join(OUT, 'report.json')}`);
process.exit(summary.fail ? 1 : 0);
