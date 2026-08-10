import { createGlobalStyle } from "styled-components";

export const theme = {
    color: {
        bg: "#F4F5F7",
        surface: "#FFFFFF",
        border: "#DFE1E6",
        text: "#172B4D",
        textMuted: "#6B778C",
        primary: "#0052CC",
        primaryHover: "#0747A6",
        primarySoft: "rgba(0, 82, 204, 0.08)",
        danger: "#DE350B",
        dangerSoft: "rgba(222, 53, 11, 0.08)",
        success: "#00875A",
        warn: "#FF991F",
    },
    radius: "3px",
    font: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif`,
};

export const GlobalStyle = createGlobalStyle`
  * { box-sizing: border-box; }
  html, body, #root { height: 100%; margin: 0; }
  html { scroll-behavior: smooth; }
  body {
    font-family: ${(p) => p.theme.font};
    background: ${(p) => p.theme.color.bg};
    color: ${(p) => p.theme.color.text};
    font-size: 14px;
  }
  button, input, textarea, select { font-family: inherit; }
  a { color: ${(p) => p.theme.color.primary}; text-decoration: none; }
`;