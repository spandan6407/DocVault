import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { useAuth } from "../../context/useAuth";
import { docApi, userApi } from "../../api/api";
import { Button, EmptyState, Grid, PageHeader, PageTitle, Section, Card } from "../../styles/shared";
import styled from "styled-components";

const ProjectCard = styled(Card)`
  display: flex;
  flex-direction: column;
  gap: 10px;
`;

const Counts = styled.div`
  display: flex;
  gap: 12px;
  margin-top: 8px;
`;

export default function Dashboard() {
    const { user } = useAuth();
    const navigate = useNavigate();
    const [projects, setProjects] = useState([]);
    const [loading, setLoading] = useState(true);

    const loadProjects = useCallback(async () => {
        setLoading(true);
        try {
            const base = (user?.projects || []).map((p) => ({
                projectId: p.projectId,
                name: p.projectName,
                role: p.role,
                description: p.description || "",
                usersCount: null,
                docsCount: null,
            }));

            setProjects(base);

            await Promise.all(
                base.map(async (p) => {
                    try {
                        // Prefer userApi for members (admin/user endpoints), fallback to docApi if necessary
                        let membersRes;
                        try {
                            membersRes = await userApi.get(`/projects/${p.projectId}/users`);
                        } catch {
                            membersRes = await docApi.get(`/projects/${p.projectId}/members`);
                        }
                        // this is the use of the dynamic routing 
                        const docsRes = await docApi.get(`/projects/${p.projectId}/documents`);
                        setProjects((prev) =>
                            prev.map((x) =>
                                x.projectId === p.projectId
                                    ? { ...x, usersCount: (membersRes.data || []).length, docsCount: (docsRes.data || []).length }
                                    : x
                            )
                        );
                    } catch {
                        // leave counts null ....
                    }
                })
            );
        } finally {
            setLoading(false);
        }
    }, [user?.projects]);

    useEffect(() => {
        queueMicrotask(loadProjects);
    }, [loadProjects]);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Projects</PageTitle>
            </PageHeader>

            <Section>
                {loading ? (
                    <EmptyState>Loading projects...</EmptyState>
                ) : projects.length === 0 ? (
                    <EmptyState>You are not currently a member of any project.</EmptyState>
                ) : (
                    <Grid $cols="repeat(auto-fill, minmax(260px, 1fr))">
                        {projects.map((p) => (
                            <ProjectCard key={p.projectId}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                    <div>
                                        <strong>{p.name || p.projectId}</strong>
                                        <div style={{ color: "#6B778C", fontSize: 12 }}>{p.description}</div>
                                    </div>
                                    <div style={{ textAlign: "right", fontSize: 13 }}>
                                        <div style={{ color: "#6B778C", fontSize: 12 }}>Role</div>
                                        <div style={{ fontWeight: 700 }}>{p.role}</div>
                                    </div>
                                </div>

                                <Counts>
                                    <Button $variant="secondary" onClick={() => navigate(`/projects/${p.projectId}/members`)}>
                                        Users: {p.usersCount ?? "…"}
                                    </Button>
                                    <Button $variant="secondary" onClick={() => navigate(`/projects/${p.projectId}/documents`)}>
                                        Documents: {p.docsCount ?? "…"}
                                    </Button>
                                </Counts>
                            </ProjectCard>
                        ))}
                    </Grid>
                )}
            </Section>
        </DashboardLayout>
    );
}

