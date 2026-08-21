import { useCallback } from "react";
import { useNavigate, useParams } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import DocumentEditor from "../../components/DocumentEditor";

export default function WritePage() {
    const { projectId } = useParams();
    const navigate = useNavigate();

    const handleCreated = useCallback(() => {
        navigate(`/projects/${projectId}/documents`);
    }, [navigate, projectId]);

    return (
        <DashboardLayout>
            <DocumentEditor onCreated={handleCreated} activeProjectId={projectId} />
        </DashboardLayout>
    );
}

