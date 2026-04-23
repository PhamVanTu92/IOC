// ─────────────────────────────────────────────────────────────────────────────
// Jest global setup — runs after the test framework is installed
// ─────────────────────────────────────────────────────────────────────────────

// Extend Jest matchers with @testing-library/jest-dom
import '@testing-library/jest-dom';

// Polyfill crypto.randomUUID for jsdom (Node 14 compat)
if (!globalThis.crypto) {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  globalThis.crypto = require('crypto').webcrypto;
}

// Mock CSS animation (jsdom doesn't support it)
Object.defineProperty(window, 'CSS', { value: null });
Object.defineProperty(document, 'doctype', {
  value: '<!DOCTYPE html>',
});

// Silence React 18 act() warnings in tests
const originalError = console.error.bind(console);
console.error = (...args: unknown[]) => {
  if (
    typeof args[0] === 'string' &&
    (args[0].includes('ReactDOM.render') || args[0].includes('act('))
  ) {
    return;
  }
  originalError(...args);
};
