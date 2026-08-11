import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import styled from "styled-components";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi, userApi } from "../../api/api";
import {
    Button,
    ErrorText,
    Field,
    Grid,
    Input,
    Label,
    PageHeader,
    PageTitle,
    Section,
} from "../../styles/shared";

const CheckList = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 220px;
  overflow-y: auto;
  border: 1px solid ${(p) => p.theme.color.border};
  border-radius: ${(p) => p.theme.radius};
  padding: 10px;
`;

const CheckRow = styled.label`
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  cursor: pointer;

  input[type="checkbox"] {
    width: 15px;
    height: 15px;
    cursor: pointer;
  }
`;

export default function CreateUserPage() {
    const navigate = useNavigate();
    const [projects, setProjects] = useState([]);
    const [values, setValues] = useState({
        email: "",
        firstName: "",
        lastName: "",
        password: "",
    });
    const [selectedProjectIds, setSelectedProjectIds] = useState([]);
    const [errors, setErrors] = useState({});
    const [creating, setCreating] = useState(false);
    const [submitError, setSubmitError] = useState(null);

    const loadProjects = useCallback(async () => {
        const res = await docApi.get("/projects");
        setProjects(res.data);
    }, []);

    useEffect(() => {
        queueMicrotask(loadProjects);
    }, [loadProjects]);

    const handleChange = useCallback((field) => (e) => {
        setValues((p) => ({ ...p, [field]: e.target.value }));
        setErrors((p) => ({ ...p, [field]: null }));
    }, []);

    const handleToggleProject = useCallback((projectId) => {
        setSelectedProjectIds((prev) =>
            prev.includes(projectId)
                ? prev.filter((id) => id !== projectId)
                : [...prev, projectId]
        );
        setErrors((p) => ({ ...p, projects: null }));
    }, []);

    const validate = useCallback(() => {
        const next = {};
        if (!values.email.trim()) next.email = "Email is required.";
        if (!values.password.trim()) next.password = "Password is required.";
        if (selectedProjectIds.length === 0)
            next.projects = "Select at least one project.";
        return next;
    }, [values, selectedProjectIds]);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            const errs = validate();
            setErrors(errs);
            if (Object.keys(errs).length > 0) return;

            setCreating(true);
            setSubmitError(null);
            try {
                await userApi.post("/users", {
                    Email: values.email,
                    FirstName: values.firstName,
                    LastName: values.lastName,
                    Password: values.password,
                    ProjectIds: selectedProjectIds,
                });
                navigate("/admin/users");
            } catch (err) {
                setSubmitError(
                    err?.response?.data?.message || "User creation failed."
                );
            } finally {
                setCreating(false);
            }
        },
        [values, selectedProjectIds, validate, navigate]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Create User</PageTitle>
            </PageHeader>

            <Section as="form" onSubmit={handleSubmit} style={{ maxWidth: 520 }}>
                <Field>
                    <Label htmlFor="u-email">Email</Label>
                    <Input
                        id="u-email"
                        type="email"
                        value={values.email}
                        onChange={handleChange("email")}
                        $invalid={!!errors.email}
                        required
                    />
                    {errors.email && <ErrorText>{errors.email}</ErrorText>}
                </Field>

                <Grid $cols="1fr 1fr">
                    <Field>
                        <Label htmlFor="u-first">First name</Label>
                        <Input
                            id="u-first"
                            value={values.firstName}
                            onChange={handleChange("firstName")}
                        />
                    </Field>
                    <Field>
                        <Label htmlFor="u-last">Last name</Label>
                        <Input
                            id="u-last"
                            value={values.lastName}
                            onChange={handleChange("lastName")}
                        />
                    </Field>
                </Grid>

                <Field>
                    <Label htmlFor="u-password">Temporary password</Label>
                    <Input
                        id="u-password"
                        type="password"
                        value={values.password}
                        onChange={handleChange("password")}
                        $invalid={!!errors.password}
                        required
                    />
                    {errors.password && <ErrorText>{errors.password}</ErrorText>}
                </Field>

                <Field>
                    <Label>
                        Projects{" "}
                        <span style={{ color: "#6B778C", fontWeight: 400 }}>
                            — user joins each as a normal member
                        </span>
                    </Label>
                    <CheckList>
                        {projects.length === 0 ? (
                            <span style={{ color: "#6B778C", fontSize: 13 }}>
                                No projects yet.
                            </span>
                        ) : (
                            projects.map((p) => (
                                <CheckRow key={p.id}>
                                    <input
                                        type="checkbox"
                                        checked={selectedProjectIds.includes(p.id)}
                                        onChange={() => handleToggleProject(p.id)}
                                    />
                                    {p.name}
                                </CheckRow>
                            ))
                        )}
                    </CheckList>
                    {errors.projects && <ErrorText>{errors.projects}</ErrorText>}
                </Field>

                {submitError && <ErrorText>{submitError}</ErrorText>}

                <Button type="submit" disabled={creating}>
                    {creating ? "Creating..." : "Create User"}
                </Button>
            </Section>
        </DashboardLayout>
    );
}