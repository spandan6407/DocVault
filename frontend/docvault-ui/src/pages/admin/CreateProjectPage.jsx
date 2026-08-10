import { useCallback, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import { Button, Field, Input, Label, PageHeader, PageTitle, Section } from "../../styles/shared";

export default function CreateProjectPage() {
    const navigate = useNavigate();
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [creating, setCreating] = useState(false);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            setCreating(true);
            try {
                await docApi.post("/projects", { Name: name, Description: description });
                navigate("/admin/projects");
            } finally {
                setCreating(false);
            }
        },
        [name, description, navigate]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Create Project</PageTitle>
            </PageHeader>

            <Section as="form" onSubmit={handleSubmit} style={{ maxWidth: 480 }}>
                <Field>
                    <Label htmlFor="p-name">Name</Label>
                    <Input id="p-name" value={name} onChange={(e) => setName(e.target.value)} required />
                </Field>
                <Field>
                    <Label htmlFor="p-desc">Description</Label>
                    <Input id="p-desc" value={description} onChange={(e) => setDescription(e.target.value)} />
                </Field>
                <Button type="submit" disabled={creating}>
                    {creating ? "Creating..." : "Create Project"}
                </Button>
            </Section>
        </DashboardLayout>
    );
}