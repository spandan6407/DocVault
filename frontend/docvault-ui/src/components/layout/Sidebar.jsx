import { memo, useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import styled from "styled-components";
import { useAuth } from "../../context/useAuth";

const Nav = styled.nav`
  width: ${(p) => (p.$open ? "232px" : "0px")} ;
  min-width: ${(p) => (p.$open ? "232px" : "0px")} ;
  flex-shrink: 0;
  background: ${(p) => p.theme.color.surface};
  border-right: ${(p) => (p.$open ? `1px solid ${p.theme.color.border}` : "none")};
  padding: ${(p) => (p.$open ? "16px 8px" : "0px")};
  height: 100%;
  overflow: ${(p) => (p.$open ? "auto" : "hidden")};
  transition: width 180ms ease, padding 180ms ease, border-right 180ms ease, opacity 180ms ease;
  opacity: ${(p) => (p.$open ? 1 : 0)};
  pointer-events: ${(p) => (p.$open ? "auto" : "none")};
  text-align: left;
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
        { to: "/admin/projects", label: "Projects" },
        { to: "/admin/users", label: "Users" },
        { to: "/admin/requests", label: "Change Requests" },
    ],
    // remove this one as we dont have the seperate role for the 


    ProjectHead: [
        { to: "/projects", label: "Dashboard", end: true },
        { to: "/projects", label: "Projects" },
        { to: "/projects/change", label: "Change Project" },
    ],
    User: [
        { to: "/projects", label: "Dashboard", end: true },
        { to: "/projects", label: "Projects" },
        { to: "/projects/change", label: "Change Project" },
    ],
};

function Sidebar({ isOpen = true }) {
    const { user } = useAuth();
    const links = useMemo(() => LINKS_BY_ROLE[user?.role] || [], [user?.role]);
    const [projectsOpen, setProjectsOpen] = useState(false);

    const ProjectsHeader = styled.div`
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 12px;
      margin-bottom: 2px;
      border-radius: ${(p) => p.theme.radius};
      font-size: 13px;
      font-weight: 500;
      color: ${(p) => p.theme.color.text};
      cursor: pointer;
      &:hover { background: ${(p) => p.theme.color.bg}; }
    `;

    const ProjectList = styled.div`
      margin: 6px 0 12px 8px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    `;

    const ProjectItem = styled(NavLink)`
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 6px 10px;
      border-radius: ${(p) => p.theme.radius};
      color: ${(p) => p.theme.color.text};
      font-size: 13px;
      text-decoration: none;
      &.active { background: ${(p) => p.theme.color.primarySoft}; color: ${(p) => p.theme.color.primary}; font-weight: 600; }
      &:hover:not(.active) { background: ${(p) => p.theme.color.bg}; }
    `;

    const ProjectRole = styled.span`
      font-size: 12px;
      color: ${(p) => p.theme.color.primary};
      font-weight: 700;
      margin-left: 8px;
    `;

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
        <Nav $open={isOpen}>
            <Brand>DocVault</Brand>
            {links.map((link) => {
                // Replace the plain "Projects" link with an expandable projects dropdown for normal users
                if ((link.to === "/projects" || link.to === "/projects") && link.label === "Projects" && (user?.role === "ProjectHead" || user?.role === "User")) {
                    return (
                        <div key={`projects-dropdown`}>
                            <ProjectsHeader onClick={() => setProjectsOpen((v) => !v)}>
                                <div>Projects</div>
                                <div>{projectsOpen ? "▾" : "▸"}</div>
                            </ProjectsHeader>
                            {projectsOpen && (
                                <ProjectList>
                                    {(user?.projects || []).map((p) => (
                                        <ProjectItem key={String(p.projectId)} to={`/projects/${p.projectId}/documents`} onClick={handleClick(`/projects/${p.projectId}/documents`)}>
                                            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{p.projectName || String(p.projectId).slice(0,8)}</span>
                                            <ProjectRole>{p.role}</ProjectRole>
                                        </ProjectItem>
                                    ))}
                                </ProjectList>
                            )}
                        </div>
                    );
                }

                return (
                    <NavItem key={`${link.to}-${link.label}`} to={link.to} end={link.end} onClick={handleClick(link.to)}>
                        {link.label}
                    </NavItem>
                );
            })}
        </Nav>
    );
}

export default memo(Sidebar);

