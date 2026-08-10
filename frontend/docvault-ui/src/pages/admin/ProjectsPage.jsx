import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import { Button, EmptyState, List, ListRow, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function ProjectsPage() {
    const navigate = useNavigate();
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);

    const loadProjects = useCallback(async () => {
        setLoading(true);
        try {
            const res = await docApi.get("/projects");
            setProjects(res.data);
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
                <Button onClick={() => navigate("/admin/projects/new")}>+ Create Project</Button>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : projects.length === 0 ? (
                    <EmptyState>No projects yet.</EmptyState>
                ) : (
                    <List>
                        {projects.map((p) => (
                            <ListRow key={p.id}>
                                <div>
                                    <strong>{p.name}</strong>
                                    <div style={{ color: "#6B778C", fontSize: 12 }}>{p.description}</div>
                                </div>
                            </ListRow>
                        ))}
                    </List>
                )}
            </Section>
        </DashboardLayout>
    );
}