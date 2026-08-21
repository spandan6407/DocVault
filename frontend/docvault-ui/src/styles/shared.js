import styled from "styled-components";

// we have to take the use of this thing inside the 

export const PageHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
`;

export const PageTitle = styled.h1`
  font-size: 20px;
  font-weight: 600;
  margin: 0;
`;

export const SectionTitle = styled.h2`
  font-size: 15px;
  font-weight: 600;
  margin: 0 0 12px;
  color: ${(p) => p.theme.color.text};
`;

export const Card = styled.div`
  background: ${(p) => p.theme.color.surface};
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  padding: 20px;
`;

export const Section = styled(Card)`
  margin-bottom: 20px;
`;

export const Grid = styled.div`
  display: grid;
  grid-template-columns: ${(p) => p.$cols || "1fr"};
  gap: 20px;
`;

export const Button = styled.button`
  padding: 8px 14px;
  border-radius: ${(p) => p.theme.radius};
  border: 1px solid transparent;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.12s ease;

  background: ${(p) =>
        p.$variant === "danger"
            ? p.theme.color.danger
            : p.$variant === "secondary"
                ? "transparent"
                : p.theme.color.primary};
  color: ${(p) => (p.$variant === "secondary" ? p.theme.color.text : "#fff")};
  border-color: ${(p) => (p.$variant === "secondary" ? p.theme.color.border : "transparent")};

  &:hover:not(:disabled) {
    background: ${(p) =>
        p.$variant === "danger"
            ? "#BF2600"
            : p.$variant === "secondary"
                ? p.theme.color.bg
                : p.theme.color.primaryHover};
  }
  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
`;

export const Badge = styled.span`
  display: inline-block;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  background: ${(p) =>
        p.$tone === "danger"
            ? p.theme.color.dangerSoft
            : p.$tone === "success"
                ? "rgba(0,135,90,0.1)"
                : p.theme.color.primarySoft};
  color: ${(p) =>
        p.$tone === "danger"
            ? p.theme.color.danger
            : p.$tone === "success"
                ? p.theme.color.success
                : p.theme.color.primary};
`;

export const StatCard = styled.button`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
  padding: 18px 20px;
  background: ${(p) => p.theme.color.surface};
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  cursor: pointer;
  text-align: left;
  transition: border-color 0.12s ease;

  &:hover {
    border-color: ${(p) => p.theme.color.primary};
  }
`;

export const StatNumber = styled.div`
  font-size: 28px;
  font-weight: 700;
  color: ${(p) => p.theme.color.primary};
`;

export const StatLabel = styled.div`
  font-size: 12px;
  font-weight: 600;
  color: ${(p) => p.theme.color.textMuted};
  text-transform: uppercase;
  letter-spacing: 0.03em;
`;

export const Toolbar = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
`;

export const Table = styled.table`
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
`;

export const Th = styled.th`
  text-align: left;
  padding: 10px 12px;
  background: ${(p) => p.theme.color.bg};
  border-bottom: 1px solid ${(p) => p.theme.color.border};
  font-size: 12px;
  font-weight: 600;
  color: ${(p) => p.theme.color.textMuted};
  text-transform: uppercase;
  letter-spacing: 0.03em;
`;

export const Td = styled.td`
  padding: 10px 12px;
  border-bottom: 1px solid ${(p) => p.theme.color.border};
  vertical-align: middle;
`;

export const Tr = styled.tr`
  &:hover {
    background: ${(p) => p.theme.color.bg};
  }
`;

export const Pagination = styled.div`
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 14px;
  font-size: 12px;
  color: ${(p) => p.theme.color.textMuted};
`;

export const MenuWrap = styled.div`
  position: relative;
  display: inline-block;
`;

export const IconButton = styled.button`
  width: 28px;
  height: 28px;
  border-radius: ${(p) => p.theme.radius};
  border: 1px solid transparent;
  background: transparent;
  font-size: 16px;
  line-height: 1;
  cursor: pointer;
  color: ${(p) => p.theme.color.text};

  &:hover {
    background: ${(p) => p.theme.color.bg};
    border-color: ${(p) => p.theme.color.border};
  }
`;

export const Menu = styled.div`
  position: absolute;
  right: 0;
  top: calc(100% + 4px);
  min-width: 140px;
  background: ${(p) => p.theme.color.surface};
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  box-shadow: 0 4px 12px rgba(9, 30, 66, 0.15);
  z-index: 10;
  overflow: hidden;
`;

export const MenuItem = styled.button`
  display: block;
  width: 100%;
  text-align: left;
  padding: 8px 12px;
  border: none;
  background: transparent;
  font-size: 13px;
  cursor: pointer;
  color: ${(p) => (p.$danger ? p.theme.color.danger : p.theme.color.text)};

  &:hover {
    background: ${(p) => p.theme.color.bg};
  }
`;

export const Field = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 12px;
`;

export const Label = styled.label`
  font-size: 12px;
  font-weight: 600;
  color: ${(p) => p.theme.color.textMuted};
`;

export const Input = styled.input`
  padding: 8px 10px;
  border: 1px solid ${(p) => (p.$invalid ? p.theme.color.danger : p.theme.color.border)};
  border-radius: ${(p) => p.theme.radius};
  font-size: 13px;
  outline: none;
  &:focus {
    border-color: ${(p) => p.theme.color.primary};
    box-shadow: 0 0 0 2px ${(p) => p.theme.color.primarySoft};
  }
`;

export const SearchInput = styled(Input)`
  flex: 1;
  max-width: 320px;
`;

export const Select = styled.select`
  padding: 8px 10px;
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  font-size: 13px;
  background: #fff;
`;

export const ErrorText = styled.div`
  font-size: 12px;
  color: ${(p) => p.theme.color.danger};
  margin-top: 4px;
`;

export const EmptyState = styled.div`
  padding: 32px;
  text-align: center;
  color: ${(p) => p.theme.color.textMuted};
  font-size: 13px;
`;

export const List = styled.ul`
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
`;

export const ListRow = styled.li`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
`;


