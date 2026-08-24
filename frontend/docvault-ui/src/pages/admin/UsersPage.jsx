import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
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
    Toolbar,
    Tr,
} from "../../styles/shared";
import EditUserProjectsModal from "../../components/EditUserProjectsModal";

const PAGE_SIZE = 4;
const ROLES = ["All Roles", "ProjectHead", "User"];

function ProjectBadges({ projects }) {
    if (!projects || projects.length === 0)
        return <span style={{ color: "#6B778C" }}>—</span>;
    return (
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
            {projects.map((p) => (
                <Badge key={p.projectId} $tone={p.role === "ProjectHead" ? "success" : undefined}>
                    {p.projectName || p.projectId.slice(0, 8)} · {p.role}
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
                ?
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
    const navigate = useNavigate();
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState("");
    const [projectFilter, setProjectFilter] = useState("all");
    const [roleFilter, setRoleFilter] = useState("All Roles");
    const [page, setPage] = useState(0);
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
    const pagedUsers = useMemo(
        () => filteredUsers.slice(page * PAGE_SIZE, page * PAGE_SIZE + PAGE_SIZE),
        [filteredUsers, page]
    );

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

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Users</PageTitle>
                <Button onClick={() => navigate("/admin/users/new")}>+ Create User</Button>
            </PageHeader>

            <Section>
                <Toolbar>
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
                </Toolbar>

                {error && <ErrorText>{error}</ErrorText>}

                {modalUser && (
                    <EditUserProjectsModal
                        user={modalUser}
                        onClose={handleCloseManage}
                        onSaved={handleSavedFromModal}
                    />
                )}

                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : filteredUsers.length === 0 ? (
                    <EmptyState>No users match the selected filters.</EmptyState>
                ) : (
                    <>
                        <Table>
                            <thead>
                                <tr>
                                    <Th>Name</Th>
                                    <Th>Email</Th>
                                    <Th>Projects · Role</Th>
                                    <Th style={{ width: 48 }} />
                                </tr>
                            </thead>
                            <tbody>
                                {pagedUsers.map((u) => (
                                    <Tr key={u.id}>
                                        <Td>{u.firstName} {u.lastName}</Td>
                                        <Td>{u.email}</Td>
                                        <Td>
                                            {u.isAdmin
                                                ? <Badge>Admin</Badge>
                                                : <ProjectBadges projects={u.projects} />}
                                        </Td>
                                        <Td>
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

                        <Pagination>
                            <span>
                                Page {page + 1} of {totalPages} · {filteredUsers.length} users
                            </span>
                            <Button
                                type="button"
                                $variant="secondary"
                                onClick={() => setPage((p) => Math.max(0, p - 1))}
                                disabled={page === 0}
                            >
                                Previous
                            </Button>
                            <Button
                                type="button"
                                $variant="secondary"
                                onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
                                disabled={page >= totalPages - 1}
                            >
                                Next
                            </Button>
                        </Pagination>
                    </>
                )}
            </Section>
        </DashboardLayout>
    );
}