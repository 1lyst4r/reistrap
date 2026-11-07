import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Variables
const source = readFileSync(new URL('../Analytics/src/index.js', import.meta.url), 'utf8');
const worker = (await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`)).default;

// Tests
for (const key of ['constructor', 'toString', '__proto__', 'unknown']) {
    const response = await worker.fetch(new Request(`https://example.test/metrics/post?key=${key}&value=test`), {});
    assert.equal(response.status, 400, key);
}
let writes = 0;
const env = { DB: { prepare: () => ({ bind: () => ({ run: async () => { writes++; } }) }) } };
const response = await worker.fetch(new Request('https://example.test/metrics/post?key=installAction&value=install'), env);
assert.equal(response.status, 200);
assert.equal(writes, 1);
console.log('5 analytics regression checks passed.');
