// Stub for echarts in Jest (heavy canvas/DOM dependency)
export const init = jest.fn(() => ({
  setOption: jest.fn(),
  resize: jest.fn(),
  dispose: jest.fn(),
  on: jest.fn(),
  off: jest.fn(),
}));
export const use = jest.fn();
export const registerTheme = jest.fn();
export default { init, use, registerTheme };
