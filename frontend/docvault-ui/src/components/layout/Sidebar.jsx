import { memo, useMemo } from "react";
import { NavLink } from "react-router-dom";
import styled from "styled-components";
import { useAuth } from "../../context/useAuth";

const Nav = styled.nav`
  width: 232px;
  flex-shrink: 0;
  background: ${(p) => p.theme.color.surface};
  border-right: 1px solid ${(p) => p.theme.color.border};
  padding: 16px 8px;
  height: 100%;
  overflow-y: auto;
`;

const Brand = styled.div`
  padding: 8px 12px 20px;
  font-size: 16px;
  font-weight: 700;
  color: ${(p) => p.theme.color.primary};
`;

const NavItem = styled(NavLink)`
  display: block;
  padding: 8px 12px;
  margin-bottom: 2px;
  border-radius: ${(p) => p.theme.radius};
  font-size: 13px;
  font-weight: 500;
  color: ${(p) => p.theme.color.text};

  &.active {
    background: ${(p) => p.theme.color.primarySoft};
    color: ${(p) => p.theme.color.primary};
    font-weight: 600;
  }
  &:hover:not(.active) {
    background: ${(p) => p.theme.color.bg};
  }
`;

const LINKS_BY_ROLE = {
    Admin: [
        { to: "/admin", label: "Dashboard", end: true },
        { to: "/admin/projects/new", label: "+ Create Project" },
        { to: "/admin/users/new", label: "+ Create User" },
        { to: "/admin/projects", label: "Projects" },
        { to: "/admin/users", label: "Users" },
        { to: "/admin/requests", label: "Change Requests" },
    ],
    ProjectHead: [
        { to: "/project-head", label: "Dashboard", end: true },
        { to: "/project-head#documents", label: "Documents" },
        { to: "/project-head#members", label: "Members" },
    ],
    User: [
        { to: "/user", label: "Dashboard", end: true },
        { to: "/user#documents", label: "Documents" },
    ],
};

function Sidebar() {
    const { user } = useAuth();
    const links = useMemo(() => LINKS_BY_ROLE[user?.role] || [], [user?.role]);

    const handleClick = (to) => (e) => {
        const hashIndex = to.indexOf("#");
        if (hashIndex === -1) return;
        const id = to.slice(hashIndex + 1);
        const el = document.getElementById(id);
        if (el) {
            e.preventDefault();
            el.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    };

    return (
        <Nav>
            <Brand>DocVault</Brand>
            {links.map((link) => (
                <NavItem key={link.to} to={link.to} end={link.end} onClick={handleClick(link.to)}>
                    {link.label}
                </NavItem>
            ))}
        </Nav>
    );
}

export default memo(Sidebar);