import { memo, useCallback } from "react";
import styled from "styled-components";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/useAuth";
import { Badge, Button } from "../../styles/shared";

const Bar = styled.header`
  height: 52px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  background: ${(p) => p.theme.color.surface};
  border-bottom: 1px solid ${(p) => p.theme.color.border};
`;

const UserBlock = styled.div`
  display: flex;
  align-items: center;
  gap: 10px;
`;

const Email = styled.span`
  font-size: 13px;
  color: ${(p) => p.theme.color.textMuted};
`;

function TopBar() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = useCallback(() => {
        logout?.();
        navigate("/login");
    }, [logout, navigate]);

    return (
        <Bar>
            <div />
            <UserBlock>
                <Email>{user?.email}</Email>
                <Badge>{user?.role}</Badge>
                <Button $variant="secondary" onClick={handleLogout}>
                    Log out
                </Button>
            </UserBlock>
        </Bar>
    );
}

export default memo(TopBar);