import { useCallback, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import {
    Button,
    ErrorText,
    Field,
    Input,
    Label,
    PageHeader,
    PageTitle,
    Section,
} from "../../styles/shared";

export default function CreateProjectPage() {
    const navigate = useNavigate();
    const [creating, setCreating] = useState(false);
    const [submitError, setSubmitError] = useState(null);

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm({
        mode: "onChange",
        defaultValues: {
            name: "",
            description: "",
        },
    });


    console.log("the create page is loaded");

    const handleCreateProject = useCallback(
        async (values) => {
            setCreating(true);
            setSubmitError(null);

            try {
                await docApi.post("/projects", {
                    Name: values.name.trim(),
                    Description: values.description.trim(),
                });

                navigate("/admin/projects");
            } catch (err) {
                setSubmitError(
                    err?.response?.data?.message ||
                    "Project creation failed."
                );
            } finally {
                setCreating(false);
            }
        },
        [navigate]
    );

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Create Project</PageTitle>
            </PageHeader>

            <Section
                as="form"
                onSubmit={handleSubmit(handleCreateProject)}
                style={{ maxWidth: 480 }}
                noValidate
            >
                <Field>
                    <Label htmlFor="p-name">Name</Label>

                    <Input
                        id="p-name"
                        $invalid={!!errors.name}
                        {...register("name", {
                            required: "Project name is required.",

                            validate: {
                                notOnlySpaces: (value) =>
                                    value.trim().length > 0 ||
                                    "Project name cannot be empty.",

                                minLength: (value) =>
                                    value.trim().length >= 3 ||
                                    "Project name must be at least 3 characters.",

                                maxLength: (value) =>
                                    value.trim().length <= 100 ||
                                    "Project name cannot exceed 100 characters.",
                            },
                        })}
                    />

                    {errors.name && (
                        <ErrorText>{errors.name.message}</ErrorText>
                    )}
                </Field>

                <Field>
                    <Label htmlFor="p-desc">Description</Label>

                    <Input
                        id="p-desc"
                        $invalid={!!errors.description}
                        {...register("description", {
                            validate: {
                                maxLength: (value) =>
                                    value.trim().length <= 500 ||
                                    "Description cannot exceed 500 characters.",

                                notOnlySpaces: (value) =>
                                    value.length === 0 ||
                                    value.trim().length > 0 ||
                                    "Description cannot contain only spaces.",
                            },
                        })}
                    />

                    {errors.description && (
                        <ErrorText>
                            {errors.description.message}
                        </ErrorText>
                    )}
                </Field>

                {submitError && <ErrorText>{submitError}</ErrorText>}

                <Button type="submit" disabled={creating}>
                    {creating ? "Creating..." : "Create Project"}
                </Button>
            </Section>
        </DashboardLayout>
    );
}

// how to take the use of the React.Memo
