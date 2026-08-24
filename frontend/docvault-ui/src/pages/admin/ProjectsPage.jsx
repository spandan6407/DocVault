import { useCallback, useEffect, useState, useMemo } from "react";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import {
    Button,
    EmptyState,
    ErrorText,
    Table,
    Th,
    Td,
    Tr,
    Pagination,
    PageHeader,
    PageTitle,
    Section,
} from "../../styles/shared";
import CreateProjectModal from "../../components/CreateProjectModal";
import { ProjectsToolbar } from "../../styles/pages/projects";

const PAGE_SIZE = 6;

export default function ProjectsPage() {
    // navigation not required; actions are modal-based

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

    const totalPages = Math.max(1, Math.ceil(projects.length / PAGE_SIZE));
    const [page, setPage] = useState(0);
    const [showCreate, setShowCreate] = useState(false);

    const paged = useMemo(() => projects.slice(page * PAGE_SIZE, page * PAGE_SIZE + PAGE_SIZE), [projects, page]);

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Projects</PageTitle>
            </PageHeader>

            <Section>
                <ProjectsToolbar>
                    <div />
                    <div style={{ marginLeft: 'auto' }}>
                        <Button onClick={() => setShowCreate(true)}>+ Create Project</Button>
                    </div>
                </ProjectsToolbar>
                {loading ? (
                    <EmptyState>Loading...</EmptyState>
                ) : error ? (
                    <ErrorText>{error}</ErrorText>
                ) : projects.length === 0 ? (
                    <EmptyState>No projects yet.</EmptyState>
                ) : (
                    <>
                        <div style={{ overflowX: 'auto' }}>
                            <Table>
                                <thead>
                            <tr>
                                        <Th style={{ cursor: 'default' }}>Title</Th>
                                        <Th style={{ cursor: 'default' }}>Description</Th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {paged.map(p => (
                                        <Tr key={p.id}>
                                            <Td data-label="Title"><strong>{p.name || '-'}</strong></Td>
                                            <Td data-label="Description" style={{ color: '#6B778C' }}>{p.description || '-'}</Td>
                                        </Tr>
                                    ))}
                                </tbody>
                            </Table>
                        </div>

                        <Pagination>
                            <span>Showing {projects.length} projects</span>
                            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                                {Array.from({ length: totalPages }).map((_, idx) => (
                                    <Button key={idx} $variant={idx === page ? undefined : 'secondary'} onClick={() => setPage(idx)}>{idx + 1}</Button>
                                ))}
                            </div>
                        </Pagination>
                    </>
                )}
            </Section>

            {showCreate && (
                <CreateProjectModal onClose={() => setShowCreate(false)} onCreated={() => { loadProjects(); setShowCreate(false); }} />
            )}
        </DashboardLayout>
    );
}


                                 