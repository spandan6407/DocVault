import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { userApi } from "../../api/api";
import {
    Badge,
    Button,
    EmptyState,
    Grid,
    List,
    ListRow,
    PageHeader,
    PageTitle,
    Section,
} from "../../styles/shared";

export default function UsersPage() {
    const navigate = useNavigate();
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);

    const loadUsers = useCallback(async () => {
        setLoading(true);
        try {
            const res = await userApi.get("/users");
            setUsers(res.data);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        queueMicrotask(loadUsers);
    }, [loadUsers]);

    const usersByRole = useMemo(
        () => ({
            Admin: users.filter((u) => u.role === "Admin"),
            ProjectHead: users.filter((u) => u.role === "ProjectHead"),
            User: users.filter((u) => u.role === "User"),
        }),
        [users]
    );

    const handleDeleteUser = useCallback(
        async (id) => {
            if (!window.confirm("Delete this user?")) return;
            await userApi.delete(`/users/${id}`);
            loadUsers();
        },
        [loadUsers]
    );

    const handleAssignHead = useCallback(
        async (userId) => {
            await userApi.put(`/users/${userId}/assign-project-head`);
            loadUsers();
        },
        [loadUsers]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Users</PageTitle>
                <Button onClick={() => navigate("/admin/users/new")}>+ Create User</Button>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : (
                    <Grid $cols="1fr 1fr 1fr">
                        {["Admin", "ProjectHead", "User"].map((role) => (
                            <div key={role}>
                                <Badge>{role}</Badge>
                                <List style={{ marginTop: 8 }}>
                                    {usersByRole[role].map((u) => (
                                        <ListRow key={u.id}>
                                            <div>
                                                <div>
                                                    {u.firstName} {u.lastName}
                                                </div>
                                                <div style={{ color: "#6B778C", fontSize: 12 }}>{u.email}</div>
                                            </div>
                                            {role === "User" && (
                                                <Button $variant="secondary" onClick={() => handleAssignHead(u.id)}>
                                                    Make Head
                                                </Button>
                                            )}
                                            {role !== "Admin" && (
                                                <Button $variant="danger" onClick={() => handleDeleteUser(u.id)}>
                                                    Delete
                                                </Button>
                                            )}
                                        </ListRow>
                                    ))}
                                    {usersByRole[role].length === 0 && <EmptyState>None</EmptyState>}
                                </List>
                            </div>
                        ))}
                    </Grid>
                )}
            </Section>
        </DashboardLayout>
    );
}