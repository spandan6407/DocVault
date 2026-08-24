import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { userApi } from "../../api/api";
import {
    Badge,
    Button,
    EmptyState,
    ErrorText,
    IconButton,
    Menu,
    MenuItem,
    MenuWrap,
    Pagination,
    PageHeader,
    PageTitle,
    SearchInput,
    Section,
    Select,
    Table,
    Td,
    Th,
    Tr,
} from "../../styles/shared";
import { FaEllipsisV } from 'react-icons/fa';
import { UserToolbar, UserTableWrapper } from '../../styles/pages/users';
import EditUserProjectsModal from "../../components/EditUserProjectsModal";
import CreateUserModal from "../../components/CreateUserModal";

const PAGE_SIZE = 4;
const ROLES = ["All Roles", "ProjectHead", "User"];

function ProjectBadges({ projects }) {
    if (!projects || projects.length === 0)
        return <span style={{ color: "#6B778C" }}>-</span>;
    return (
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
            {projects.map((p) => (
                <Badge key={p.projectId} $tone={undefined}>
                    {p.projectName || String(p.projectId).slice(0, 8)}
                </Badge>
            ))}
        </div>
    );
}

function ActionMenu({ user, onAssignHead, onDelete, onManage }) {
    const [view, setView] = useState("closed");
    const wrapRef = useRef(null);

    useEffect(() => {
        if (view === "closed") return;
        function handleOutside(e) {
            if (wrapRef.current && !wrapRef.current.contains(e.target)) setView("closed");
        }
        document.addEventListener("mousedown", handleOutside);
        return () => document.removeEventListener("mousedown", handleOutside);
    }, [view]);

    const eligibleProjects = (user.projects || []).filter((p) => p.role === "User");

    if (user.isAdmin) return null;

    return (
        <MenuWrap ref={wrapRef}>
            <IconButton
                type="button"
                onClick={() => setView((v) => (v === "closed" ? "root" : "closed"))}
                aria-label="Actions"
            >
                <FaEllipsisV />
            </IconButton>

            {view === "root" && (
                <Menu>
                    <MenuItem type="button" onClick={() => { setView("closed"); onManage && onManage(user); }}>
                        Manage Projects
                    </MenuItem>
                    <MenuItem
                        type="button"
                        $danger
                        onClick={() => { setView("closed"); onDelete(user.id); }}
                    >
                        Delete
                    </MenuItem>
                </Menu>
            )}

            {view === "pickProject" && (
                <Menu style={{ minWidth: 200 }}>
                    {eligibleProjects.map((p) => (
                        <MenuItem
                            key={p.projectId}
                            type="button"
                            onClick={() => { setView("closed"); onAssignHead(user.id, p.projectId); }}
                        >
                            {p.projectName || p.projectId.slice(0, 8)}
                        </MenuItem>
                    ))}
                </Menu>
            )}
        </MenuWrap>
    );
}

export default function UsersPage() {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState("");
    const [projectFilter, setProjectFilter] = useState("all");
    const [roleFilter, setRoleFilter] = useState("All Roles");
    const [page, setPage] = useState(0);
    const [showCreate, setShowCreate] = useState(false);
    const [sortBy, setSortBy] = useState(null);
    const [sortDir, setSortDir] = useState(1); // 1 asc, -1 desc
    const [error, setError] = useState("");

    // Fetch users according to current filters/search. Reusable for initial load and refresh.
    const fetchUsers = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            const trimmed = search.trim();
            const params = [];
            if (trimmed) params.push(`query=${encodeURIComponent(trimmed)}`);
            if (projectFilter !== "all") params.push(`projectId=${encodeURIComponent(projectFilter)}`);
            if (roleFilter !== "All Roles") params.push(`role=${encodeURIComponent(roleFilter)}`);

            let res;
            if (params.length > 0) {
                res = await userApi.get(`/users/search?${params.join("&")}`);
            } else {
                res = await userApi.get(`/users`);
            }

            setUsers(res.data);
            setPage(0);
        } catch (err) {
            console.error(err);
            setError(err?.response?.data?.message || "Failed to load users.");
        } finally {
            setLoading(false);
        }
    }, [search, projectFilter, roleFilter]);

    // Initial load
    useEffect(() => { queueMicrotask(fetchUsers); }, [fetchUsers]);

    // Build unique project list from all users' memberships for the project dropdown
    const [allProjects, setAllProjects] = useState([]);

    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const res = await userApi.get('/user-projects');
                if (!mounted) return;
                const projects = (res.data || []).map(p => ({ id: p.projectId, name: p.projectName }));
                setAllProjects(projects);
            } catch (err) {
                console.warn('Failed to load project list for dropdown', err);
            }
        })();
        return () => { mounted = false; };
    }, []);

    // When search, projectFilter, or roleFilter changes, call backend search endpoint with debounce
    useEffect(() => {
        let mounted = true;
        let timer = null;
        const performSearch = async () => {
            setLoading(true);
            setError("");
            try {
                const trimmed = search.trim();
                const params = [];
                if (trimmed) params.push(`query=${encodeURIComponent(trimmed)}`);
                if (projectFilter !== "all") params.push(`projectId=${encodeURIComponent(projectFilter)}`);
                if (roleFilter !== "All Roles") params.push(`role=${encodeURIComponent(roleFilter)}`);

                let res;
                if (params.length > 0) {
                    res = await userApi.get(`/users/search?${params.join("&")}`);
                } else {
                    res = await userApi.get(`/users`);
                }

                if (!mounted) return;
                setUsers(res.data);
                setPage(0);
            } catch (err) {
                console.error(err);
                setError(err?.response?.data?.message || "Failed to load users.");
            } finally {
                if (mounted) setLoading(false);
            }
        };

        timer = setTimeout(performSearch, 350);
        return () => {
            mounted = false;
            if (timer) clearTimeout(timer);
        };
    }, [search, projectFilter, roleFilter]);

    // Server performs search and filters; frontend only paginates the returned users
    const filteredUsers = useMemo(() => users, [users]);

    const totalPages = Math.max(1, Math.ceil(filteredUsers.length / PAGE_SIZE));

    const resetPage = useCallback(() => setPage(0), []);

    const handleSearchChange = useCallback((e) => { setSearch(e.target.value); resetPage(); }, [resetPage]);
    const handleProjectChange = useCallback((e) => { setProjectFilter(e.target.value); resetPage(); }, [resetPage]);
    const handleRoleChange = useCallback((e) => { setRoleFilter(e.target.value); resetPage(); }, [resetPage]);

    const handleDeleteUser = useCallback(async (id) => {
        if (!window.confirm("Delete this user?")) return;
        await userApi.delete(`/users/${id}`);
        fetchUsers();
    }, [fetchUsers]);

    const handleAssignHead = useCallback(async (userId, projectId) => {
        await userApi.put(`/users/${userId}/assign-project-head`, { ProjectId: projectId });
        fetchUsers();
    }, [fetchUsers]);

    const [modalUser, setModalUser] = useState(null);
    const handleOpenManage = useCallback((user) => setModalUser(user), []);
    const handleCloseManage = useCallback(() => setModalUser(null), []);
    const handleSavedFromModal = useCallback(() => { fetchUsers(); handleCloseManage(); }, [fetchUsers, handleCloseManage]);

    const sortedUsers = useMemo(() => {
        if (!sortBy) return filteredUsers;
        const copy = [...filteredUsers];
        copy.sort((a,b) => {
            const va = sortBy === 'name' ? `${a.firstName} ${a.lastName}`.toLowerCase() : (a[sortBy] || '').toLowerCase();
            const vb = sortBy === 'name' ? `${b.firstName} ${b.lastName}`.toLowerCase() : (b[sortBy] || '').toLowerCase();
            if (va < vb) return -1 * sortDir;
            if (va > vb) return 1 * sortDir;
            return 0;
        });
        return copy;
    }, [filteredUsers, sortBy, sortDir]);

    const pagedUsers = useMemo(
        () => sortedUsers.slice(page * PAGE_SIZE, page * PAGE_SIZE + PAGE_SIZE),
        [sortedUsers, page]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Users</PageTitle>
            </PageHeader>

            <Section>
                <UserToolbar>
                    <SearchInput
                        placeholder="Search by name or email"
                        value={search}
                        onChange={handleSearchChange}
                    />
                    <Select value={projectFilter} onChange={handleProjectChange}>
                        <option value="all">All Projects</option>
                        {allProjects.map((p) => (
                            <option key={p.id} value={p.id}>{p.name}</option>
                        ))}
                    </Select>
                    <Select value={roleFilter} onChange={handleRoleChange}>
                        {ROLES.map((r) => (
                            <option key={r} value={r}>{r}</option>
                        ))}
                    </Select>
                    <div style={{ marginLeft: 'auto' }}>
                        <Button onClick={() => setShowCreate(true)}>+ Create User</Button>
                    </div>
                </UserToolbar>

                {error && <ErrorText>{error}</ErrorText>}

                {modalUser && (
                    <EditUserProjectsModal
                        user={modalUser}
                        onClose={handleCloseManage}
                        onSaved={handleSavedFromModal}
                    />
                )}

                {showCreate && (
                    <CreateUserModal onClose={() => setShowCreate(false)} onCreated={() => { fetchUsers(); setShowCreate(false); }} />
                )}

                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : filteredUsers.length === 0 ? (
                    <EmptyState>No users match the selected filters.</EmptyState>
                ) : (
                    <>
                        <UserTableWrapper>
                        <Table>
                            <thead>
                                <tr>
                                    <Th style={{ cursor: 'pointer' }} onClick={() => { if (sortBy === 'name') setSortDir(d => -d); else { setSortBy('name'); setSortDir(1); } }}>
                                        Name <span style={{ color: '#2563EB', marginLeft: 6 }}>{sortBy === 'name' ? (sortDir === 1 ? '?' : '?') : ''}</span>
                                    </Th>
                                    <Th style={{ cursor: 'pointer' }} onClick={() => { if (sortBy === 'email') setSortDir(d => -d); else { setSortBy('email'); setSortDir(1); } }}>
                                        Email <span style={{ color: '#2563EB', marginLeft: 6 }}>{sortBy === 'email' ? (sortDir === 1 ? '?' : '?') : ''}</span>
                                    </Th>
                                    <Th>Projects</Th>
                                    <Th>Role</Th>
                                    <Th style={{ width: 48 }} />
                                </tr>
                            </thead>
                            <tbody>
                                {pagedUsers.map((u) => (
                                    <Tr key={u.id}>
                                        <Td data-label="Name">{(u.firstName || u.lastName) ? `${u.firstName || ''} ${u.lastName || ''}`.trim() : '-'}</Td>
                                        <Td data-label="Email">{u.email || '-'}</Td>
                                        <Td data-label="Projects"> 
                                            {u.projects && u.projects.length > 0 ? <ProjectBadges projects={u.projects} /> : '-'}
                                        </Td>
                                        <Td data-label="Role">
                                            {u.isAdmin ? <Badge $tone="success">Admin</Badge> : (u.role || (u.projects && u.projects[0] && u.projects[0].role) || '-')}
                                        </Td>
                                        <Td data-label="Actions">
                                            <ActionMenu
                                                user={u}
                                                onAssignHead={handleAssignHead}
                                                onDelete={handleDeleteUser}
                                                onManage={() => handleOpenManage(u)}
                                            />
                                        </Td>
                                    </Tr>
                                ))}
                            </tbody>
                        </Table>
                        </UserTableWrapper>

                        <Pagination>
                            <span>Showing {filteredUsers.length} users</span>
                            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                                {Array.from({ length: totalPages }).map((_, idx) => (
                                    <Button key={idx} $variant={idx === page ? undefined : 'secondary'} onClick={() => setPage(idx)}>{idx + 1}</Button>
                                ))}
                            </div>
                        </Pagination>
                    </>
                )}
            </Section>
        </DashboardLayout>
    );
}