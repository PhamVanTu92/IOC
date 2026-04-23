import type { Config } from 'jest';

const config: Config = {
  // ─── Preset ────────────────────────────────────────────────────────────────
  preset: 'ts-jest',
  testEnvironment: 'jest-environment-jsdom',

  // ─── Test discovery ────────────────────────────────────────────────────────
  // Pick up tests from both src/ and the shared tests/frontend/unit/ dir
  roots: ['<rootDir>/src', '<rootDir>/../../tests/frontend/unit'],
  testMatch: ['**/*.test.ts', '**/*.test.tsx'],

  // ─── TypeScript via ts-jest ────────────────────────────────────────────────
  transform: {
    '^.+\\.(ts|tsx)$': [
      'ts-jest',
      {
        tsconfig: {
          // Override some strict options that conflict with Jest's module system
          moduleResolution: 'node',
          allowImportingTsExtensions: false,
          jsx: 'react-jsx',
        },
      },
    ],
  },

  // ─── Module path aliases (mirrors tsconfig.json paths) ────────────────────
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@core/(.*)$': '<rootDir>/src/core/$1',
    '^@plugins/(.*)$': '<rootDir>/src/plugins/$1',
    '^@shared/(.*)$': '<rootDir>/src/shared/$1',
    // Stub static assets
    '\\.(css|less|scss|svg|png|jpg|gif|webp)$': '<rootDir>/src/__mocks__/fileMock.ts',
    // Stub echarts-for-react in unit tests (heavy canvas dep)
    '^echarts-for-react$': '<rootDir>/src/__mocks__/echartsForReact.tsx',
    '^echarts(.*)$': '<rootDir>/src/__mocks__/echarts.ts',
  },

  // ─── Setup files ───────────────────────────────────────────────────────────
  setupFilesAfterEnv: ['<rootDir>/src/setupTests.ts'],

  // ─── Coverage ──────────────────────────────────────────────────────────────
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!src/main.tsx',
    '!src/App.tsx',
    '!src/**/*.d.ts',
    '!src/__mocks__/**',
  ],
  coverageThreshold: {
    global: {
      branches: 70,
      functions: 75,
      lines: 75,
      statements: 75,
    },
  },

  // ─── Misc ──────────────────────────────────────────────────────────────────
  clearMocks: true,
  verbose: true,
};

export default config;
