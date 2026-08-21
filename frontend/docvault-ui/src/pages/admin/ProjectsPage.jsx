import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import {
    Button,
    EmptyState,
    ErrorText,
    List,
    ListRow,
    PageHeader,
    PageTitle,
    Section,
} from "../../styles/shared";

export default function ProjectsPage() {
    const navigate = useNavigate();

    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const loadProjects = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const res = await docApi.get("/projects");
            setProjects(res.data);
        } catch (err) {
            console.log("Failed to load projects:", err);

            setError(
                err?.response?.data?.message ||
                "Failed to load projects."
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        queueMicrotask(loadProjects);
    }, [loadProjects]);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Projects</PageTitle>

                <Button
                    onClick={() => navigate("/admin/projects/new")}
                >
                    + Create Project
                </Button>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : error ? (
                    <ErrorText>{error}</ErrorText>
                ) : projects.length === 0 ? (
                    <EmptyState>No projects yet.</EmptyState>
                ) : (
                    <List>
                        {projects.map((p) => (
                            <ListRow key={p.id}>
                                <div>
                                    <strong>{p.name}</strong>

                                    <div
                                        style={{
                                            color: "#6B778C",
                                            fontSize: 12,
                                        }}
                                    >
                                        {p.description}
                                    </div>
                                </div>
                            </ListRow>
                        ))}
                    </List>
                )}
            </Section>
        </DashboardLayout>
    );
}


                                 