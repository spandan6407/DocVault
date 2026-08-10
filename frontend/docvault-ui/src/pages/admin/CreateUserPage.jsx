import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi, userApi } from "../../api/api";
import { Button, Field, Grid, Input, Label, PageHeader, PageTitle, Section, Select } from "../../styles/shared";

export default function CreateUserPage() {
    const navigate = useNavigate();
    const [projects, setProjects] = useState([]);
    const [newUser, setNewUser] = useState({ email: "", firstName: "", lastName: "", projectId: "", password: "" });
    const [creating, setCreating] = useState(false);

    const loadProjects = useCallback(async () => {
        const res = await docApi.get("/projects");
        setProjects(res.data);
    }, []);

    useEffect(() => {
        queueMicrotask(loadProjects);
    }, [loadProjects]);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            setCreating(true);
            try {
                await userApi.post("/users", {
                    Email: newUser.email,
                    FirstName: newUser.firstName,
                    LastName: newUser.lastName,
                    ProjectId: newUser.projectId,
                    Password: newUser.password,
                });
                navigate("/admin/users");
            } finally {
                setCreating(false);
            }
        },
        [newUser, navigate]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Create User</PageTitle>
            </PageHeader>

            <Section as="form" onSubmit={handleSubmit} style={{ maxWidth: 480 }}>
                <Field>
                    <Label htmlFor="u-email">Email</Label>
                    <Input
                        id="u-email"
                        type="email"
                        value={newUser.email}
                        onChange={(e) => setNewUser((p) => ({ ...p, email: e.target.value }))}
                        required
                    />
                </Field>
                <Grid $cols="1fr 1fr">
                    <Field>
                        <Label htmlFor="u-first">First name</Label>
                        <Input
                            id="u-first"
                            value={newUser.firstName}
                            onChange={(e) => setNewUser((p) => ({ ...p, firstName: e.target.value }))}
                        />
                    </Field>
                    <Field>
                        <Label htmlFor="u-last">Last name</Label>
                        <Input
                            id="u-last"
                            value={newUser.lastName}
                            onChange={(e) => setNewUser((p) => ({ ...p, lastName: e.target.value }))}
                        />
                    </Field>
                </Grid>
                <Field>
                    <Label htmlFor="u-project">Project</Label>
                    <Select
                        id="u-project"
                        value={newUser.projectId}
                        onChange={(e) => setNewUser((p) => ({ ...p, projectId: e.target.value }))}
                        required
                    >
                        <option value="">Select a project</option>
                        {projects.map((p) => (
                            <option key={p.id} value={p.id}>
                                {p.name}
                            </option>
                        ))}
                    </Select>
                </Field>
                <Field>
                    <Label htmlFor="u-password">Temporary password</Label>
                    <Input
                        id="u-password"
                        type="password"
                        value={newUser.password}
                        onChange={(e) => setNewUser((p) => ({ ...p, password: e.target.value }))}
                        required
                    />
                </Field>
                <Button type="submit" disabled={creating}>
                    {creating ? "Creating..." : "Create User"}
                </Button>
            </Section>
        </DashboardLayout>
    );
}