import { memo, useCallback, useState, useRef, useEffect } from "react";
import { FaBars, FaUserCircle } from 'react-icons/fa';
import styled from "styled-components";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/useAuth";
import { IconButton, Menu, MenuItem } from "../../styles/shared";

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

function TopBar({ onToggleSidebar }) {
    const { user, logout } = useAuth();
    const navigate = useNavigate();
    const [menuOpen, setMenuOpen] = useState(false);
    const wrapRef = useRef(null);

    useEffect(() => {
        if (!menuOpen) return;
        function handleOutside(e) {
            if (wrapRef.current && !wrapRef.current.contains(e.target)) setMenuOpen(false);
        }
        document.addEventListener('mousedown', handleOutside);
        return () => document.removeEventListener('mousedown', handleOutside);
    }, [menuOpen]);

    const handleLogout = useCallback(() => {
        logout?.();
        navigate('/login');
    }, [logout, navigate]);

    return (
        <Bar>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <IconButton aria-label="Toggle sidebar" onClick={onToggleSidebar}>
                    <FaBars />
                </IconButton>
                <div />
            </div>

            <UserBlock ref={wrapRef} style={{ position: 'relative' }}>
                <Email style={{ display: 'none' }}>{user?.email}</Email>
                <IconButton aria-label="User menu" onClick={() => setMenuOpen((v) => !v)}>
                    <FaUserCircle />
                </IconButton>
                {menuOpen && (
                    <Menu style={{ right: 0, top: 'calc(100% + 6px)' }}>
                        <MenuItem type="button" style={{ cursor: 'default' }}>{user?.email}</MenuItem>
                        <MenuItem type="button" onClick={() => { setMenuOpen(false); navigate('/profile'); }}>Profile</MenuItem>
                        <MenuItem type="button" onClick={() => { setMenuOpen(false); handleLogout(); }}>Sign out</MenuItem>
                    </Menu>
                )}
            </UserBlock>
        </Bar>
    );
}

export default memo(TopBar);
// use of the memo .....