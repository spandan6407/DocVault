import { useCallback, useState } from "react";
import styled from "styled-components";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useAuth } from "../context/useAuth";
import {
    Button,
    Card,
    ErrorText,
    Field,
    Input,
    Label,
} from "../styles/shared";

const Wrap = styled.div`
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: ${(p) => p.theme.color.bg};
`;

const LoginCard = styled(Card)`
  width: 340px;
`;

const Brand = styled.div`
  font-size: 18px;
  font-weight: 700;
  color: ${(p) => p.theme.color.primary};
  margin-bottom: 20px;
`;

const ROLE_HOME = {
    Admin: "/admin",
    ProjectHead: "/projects",
    User: "/projects",
};

export default function Login() {
    const { login } = useAuth();
    const navigate = useNavigate();

    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm({
        mode: "onChange",
        defaultValues: {
            email: "",
            password: "",
        },
    });

    const handleLogin = useCallback(
        async (values) => {
            setError(null);
            setLoading(true);

            try {
                const user = await login(values.email, values.password);
                navigate(ROLE_HOME[user.role] || "/login");
            } catch (err) {
                setError(
                    err?.response?.data?.message ||
                    "Invalid email or password."
                );
            } finally {
                setLoading(false);
            }
        },
        [login, navigate]
    );

    return (
        <Wrap>
            <LoginCard>
                <Brand>DocVault</Brand>

                <form onSubmit={handleSubmit(handleLogin)} noValidate>
                    <Field>
                        <Label htmlFor="email">Email</Label>

                        <Input
                            id="email"
                            type="email"
                            $invalid={!!errors.email}
                            {...register("email", {
                                required: "Email is required.",

                                validate: {
                                    noSpaces: (value) =>
                                        value === value.trim() ||
                                        "Email cannot contain spaces.",

                                    validEmail: (value) =>
                                        /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
                                            value
                                        ) ||
                                        "Please enter a valid email address.",
                                },
                            })}
                        />

                        {errors.email && (
                            <ErrorText>
                                {errors.email.message}
                            </ErrorText>
                        )}
                    </Field>

                    <Field>
                        <Label htmlFor="password">Password</Label>

                        <Input
                            id="password"
                            type="password"
                            $invalid={!!errors.password}
                            {...register("password", {
                                required: "Password is required.",

                                validate: {
                                    noSpaces: (value) =>
                                        !/\s/.test(value) ||
                                        "Password cannot contain spaces.",

                                    minLength: (value) =>
                                        value.length >= 8 ||
                                        "Password must be at least 8 characters.",

                                    uppercase: (value) =>
                                        /[A-Z]/.test(value) ||
                                        "Password must contain an uppercase letter.",

                                    lowercase: (value) =>
                                        /[a-z]/.test(value) ||
                                        "Password must contain a lowercase letter.",

                                    number: (value) =>
                                        /\d/.test(value) ||
                                        "Password must contain a number.",

                                    specialCharacter: (value) =>
                                        /[^A-Za-z0-9]/.test(value) ||
                                        "Password must contain a special character.",
                                },
                            })}
                        />

                        {errors.password && (
                            <ErrorText>
                                {errors.password.message}
                            </ErrorText>
                        )}
                    </Field>

                    {error && <ErrorText>{error}</ErrorText>}

                    <Button
                        type="submit"
                        disabled={loading}
                        style={{
                            width: "100%",
                            marginTop: 8,
                        }}
                    >
                        {loading ? "Signing in..." : "Sign in"}
                    </Button>
                </form>
            </LoginCard>
        </Wrap>
    );
}