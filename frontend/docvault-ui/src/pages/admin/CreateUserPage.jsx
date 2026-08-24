import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
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
    const [selectedProjectIds, setSelectedProjectIds] = useState([]);
    const [creating, setCreating] = useState(false);
    const [submitError, setSubmitError] = useState("");
    const [projectError, setProjectError] = useState("");

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm({
        mode: "onChange",
    });

    
    useEffect(() => {
        async function loadProjects() {
            try {
                const response = await docApi.get("/projects");
                setProjects(response.data);
            } catch (error) {
                console.log("Failed to load projects:", error);
            }
        }

        loadProjects();
    }, []);

    
    function handleProjectChange(projectId) {
        if (selectedProjectIds.includes(projectId)) {
            setSelectedProjectIds(
                selectedProjectIds.filter((id) => id !== projectId)
            );
        } else {
            setSelectedProjectIds([
                ...selectedProjectIds,
                projectId,
            ]);
        }

        
        setProjectError("");
    }

    
    async function onSubmit(data) {
        setSubmitError("");

        
        if (selectedProjectIds.length === 0) {
            setProjectError("Please select at least one project.");
            return;
        }

        setProjectError("");
        setCreating(true);

        try {
            await userApi.post("/users", {
                Email: data.email,
                FirstName: data.firstName,
                LastName: data.lastName,
                Password: data.password,
                ProjectIds: selectedProjectIds,
            });

            navigate("/admin/users");
        } catch (error) {
            console.log("User creation failed:", error);

            setSubmitError(
                error?.response?.data?.message ||
                "User creation failed."
            );
        } finally {
            setCreating(false);
        }
    }

    return (
        <DashboardLayout>
            <PageHeader>
                <PageTitle>Create User</PageTitle>
            </PageHeader>

            <Section
                as="form"
                onSubmit={handleSubmit(onSubmit)}
                style={{ maxWidth: 520 }}
            >
                
                <Field>
                    <Label htmlFor="u-email">
                        Email
                    </Label>

                    <Input
                        id="u-email"
                        type="email"
                        $invalid={!!errors.email}
                        {...register("email", {
                            required: "Email is required.",

                            pattern: {
                                value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                                message: "Enter a valid email address.",
                            },
                        })}
                    />

                    {errors.email && (
                        <ErrorText>
                            {errors.email.message}
                        </ErrorText>
                    )}
                </Field>

                
                <Grid $cols="1fr 1fr">
                    <Field>
                        <Label htmlFor="u-first">
                            First name
                        </Label>

                        <Input
                            id="u-first"
                            type="text"
                            $invalid={!!errors.firstName}
                            {...register("firstName", {
                                required: "First name is required.",

                                pattern: {
                                    value: /^[A-Za-z ]+$/,
                                    message:
                                        "First name should contain only letters.",
                                },
                            })}
                        />

                        {errors.firstName && (
                            <ErrorText>
                                {errors.firstName.message}
                            </ErrorText>
                        )}
                    </Field>

                    <Field>
                        <Label htmlFor="u-last">
                            Last name
                        </Label>

                        <Input
                            id="u-last"
                            type="text"
                            $invalid={!!errors.lastName}
                            {...register("lastName", {
                                required: "Last name is required.",

                                pattern: {
                                    value: /^[A-Za-z ]+$/,
                                    message:
                                        "Last name should contain only letters.",
                                },
                            })}
                        />

                        {errors.lastName && (
                            <ErrorText>
                                {errors.lastName.message}
                            </ErrorText>
                        )}
                    </Field>
                </Grid>

                
                <Field>
                    <Label htmlFor="u-password">
                        Temporary password
                    </Label>

                    <Input
                        id="u-password"
                        type="password"
                        $invalid={!!errors.password}
                        {...register("password", {
                            required: "Password is required.",

                            minLength: {
                                value: 8,
                                message:
                                    "Password must be at least 8 characters.",
                            },

                            pattern: {
                                value:
                                    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/,
                                message:
                                    "Password must contain uppercase, lowercase, and a number.",
                            },
                        })}
                    />

                    {errors.password && (
                        <ErrorText>
                            {errors.password.message}
                        </ErrorText>
                    )}
                </Field>

                {/* Projects */}
                <Field>
                    <Label>
                        Projects{" "}
                        <span
                            style={{
                                color: "#6B778C",
                                fontWeight: 400,
                            }}
                        >
                            — user joins each as a normal member
                        </span>
                    </Label>

                    <CheckList>
                        {projects.length === 0 ? (
                            <span
                                style={{
                                    color: "#6B778C",
                                    fontSize: 13,
                                }}
                            >
                                No projects yet.
                            </span>
                        ) : (
                            projects.map((project) => (
                                <CheckRow key={project.id}>
                                    <input
                                        type="checkbox"
                                        checked={selectedProjectIds.includes(
                                            project.id
                                        )}
                                        onChange={() =>
                                            handleProjectChange(
                                                project.id
                                            )
                                        }
                                    />

                                    {project.name}
                                </CheckRow>
                            ))
                        )}
                    </CheckList>

                    {projectError && (
                        <ErrorText>
                            {projectError}
                        </ErrorText>
                    )}
                </Field>

                
                {submitError && (
                    <ErrorText>
                        {submitError}
                    </ErrorText>
                )}

                
                <Button
                    type="submit"
                    disabled={creating}
                >
                    {creating
                        ? "Creating..."
                        : "Create User"}
                </Button>
            </Section>
        </DashboardLayout>
    );
}