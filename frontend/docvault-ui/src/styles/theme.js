import { createGlobalStyle } from "styled-components";

export const theme = {
    color: {
        bg: "#F7F9FB",
        surface: "#FFFFFF",
        border: "#E6E9EE",
        text: "#0F1724",
        textMuted: "#556B7A",
        primary: "#2563EB",
        primaryHover: "#1E40AF",
        primarySoft: "rgba(37,99,235,0.08)",
        danger: "#EF4444",
        dangerSoft: "rgba(239,68,68,0.08)",
        success: "#10B981",
        warn: "#F59E0B",
    },
    radius: "3px",
    font: `Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif`,
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
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }
  button, input, textarea, select { font-family: inherit; }
  a { color: ${(p) => p.theme.color.primary}; text-decoration: none; }

  /* small animations */
  button { transition: all 150ms ease; }
  input, select { transition: border-color 120ms ease, box-shadow 120ms ease; }
`;