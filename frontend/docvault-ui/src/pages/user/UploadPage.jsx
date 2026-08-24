import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import DashboardLayout from "../../components/layout/DashboardLayout";
import { docApi } from "../../api/api";
import { Button, EmptyState, Field, Input, Label, PageHeader, PageTitle, Section } from "../../styles/shared";

const ALLOWED_EXTENSIONS = [".doc", ".docx", ".pdf"];
const ALLOWED_MIME_TYPES = [
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/pdf",
];

export default function UploadPage() {
    const { projectId } = useParams();
    const navigate = useNavigate();
    const [uploading, setUploading] = useState(false);
    const [serverError, setServerError] = useState(null);

    const {
        register,
        handleSubmit,
        watch,
        formState: { errors },
    } = useForm({
        mode: "onChange",
        defaultValues: {
            title: "",
            description: "",
            file: null,
        },
    });

    const watchedFile = watch("file");
    const selectedFile = watchedFile && watchedFile.length > 0 ? watchedFile[0] : null;

    const validateFile = (fileList) => {
        if (!fileList || fileList.length === 0) {
            return "Please select a file to upload.";
        }
        const file = fileList[0];
        const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();

        const extensionOk = ALLOWED_EXTENSIONS.includes(extension);
        const mimeOk = ALLOWED_MIME_TYPES.includes(file.type);

        // Some browsers/OS combos don't reliably set file.type for .doc/.docx,
        // so we accept if either the extension or MIME type checks out.
        if (!extensionOk && !mimeOk) {
            return "Only .doc, .docx, and .pdf files are allowed.";
        }
        return true;
    };

    const onSubmit = async (data) => {
        setUploading(true);
        setServerError(null);
        try {
            const form = new FormData();
            const file = data.file[0];
            form.append("File", file);
            form.append("Title", data.title || file.name);
            form.append("Description", data.description || "");
            form.append("ProjectId", projectId);
            // Do not manually set Content-Type; let the browser/axios set the correct
            // multipart boundary header. Manually setting it can cause a 400 Bad Request
            // from the server because the boundary is missing.
            await docApi.post("/documents", form);
            navigate(`/projects/${projectId}/documents`);
        } catch (err) {
            // Surface backend validation errors when available
            const responseData = err?.response?.data;
            if (!responseData) {
                setServerError("Upload failed. Please try again.");
            } else if (responseData.errors) {
                // ASP.NET ModelState validation -> { errors: { Field: [..] } }
                const messages = Object.values(responseData.errors).flat();
                setServerError(messages.join(" "));
            } else if (responseData.message) {
                setServerError(responseData.message);
            } else {
                setServerError(JSON.stringify(responseData));
            }
        } finally {
            setUploading(false);
        }
    };

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Upload Document</PageTitle>
            </PageHeader>

            <Section>
                <form onSubmit={handleSubmit(onSubmit)} noValidate>
                    <Field>
                        <Label htmlFor="upload-title">Title</Label>
                        <Input
                            id="upload-title"
                            placeholder="Document title"
                            {...register("title", {
                                maxLength: {
                                    value: 200,
                                    message: "Title must be 200 characters or fewer.",
                                },
                            })}
                        />
                        {errors.title && <EmptyState>{errors.title.message}</EmptyState>}
                    </Field>

                    <Field>
                        <Label htmlFor="upload-description">Description</Label>
                        <Input
                            id="upload-description"
                            placeholder="Optional description"
                            {...register("description", {
                                maxLength: {
                                    value: 1000,
                                    message: "Description must be 1000 characters or fewer.",
                                },
                            })}
                        />
                        {errors.description && <EmptyState>{errors.description.message}</EmptyState>}
                    </Field>

                    <Field>
                        <Label htmlFor="upload-file">File</Label>
                        <input
                            id="upload-file"
                            type="file"
                            accept=".doc,.docx,.pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/pdf"
                            {...register("file", { validate: validateFile })}
                        />
                        {selectedFile && !errors.file && (
                            <small>{selectedFile.name}</small>
                        )}
                        {errors.file && <EmptyState>{errors.file.message}</EmptyState>}
                    </Field>

                    {serverError && <EmptyState>{serverError}</EmptyState>}

                    <Button type="submit" disabled={uploading || !selectedFile}>
                        {uploading ? "Uploading..." : "Upload"}
                    </Button>
                </form>
            </Section>
        </DashboardLayout>
    );
}