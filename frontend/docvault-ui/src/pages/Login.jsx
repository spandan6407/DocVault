import { useCallback, useState } from "react";
import styled from "styled-components";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { Button, Card, ErrorText, Field, Input, Label } from "../styles/shared";

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

const ROLE_HOME = { Admin: "/admin", ProjectHead: "/project-head", User: "/user" };

export default function Login() {
    const { login } = useAuth();
    const navigate = useNavigate();

    const [values, setValues] = useState({ email: "", password: "" });
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleChange = useCallback((field) => (e) => {
        setValues((prev) => ({ ...prev, [field]: e.target.value }));
    }, []);

    const handleSubmit = useCallback(
        async (e) => {
            e.preventDefault();
            setError(null);
            setLoading(true);
            try {
                const user = await login(values.email, values.password);
                navigate(ROLE_HOME[user.role] || "/login");
            } catch (err) {
                setError(err?.response?.data?.message || "Invalid email or password.");
            } finally {
                setLoading(false);
            }
        },
        [login, navigate, values]
    );

    return (
        <Wrap>
            <LoginCard>
                <Brand>DocVault</Brand>
                <form onSubmit={handleSubmit} noValidate>
                    <Field>
                        <Label htmlFor="email">Email</Label>
                        <Input
                            id="email"
                            type="email"
                            value={values.email}
                            onChange={handleChange("email")}
                            required
                        />
                    </Field>
                    <Field>
                        <Label htmlFor="password">Password</Label>
                        <Input
                            id="password"
                            type="password"
                            value={values.password}
                            onChange={handleChange("password")}
                            required
                        />
                    </Field>
                    {error && <ErrorText>{error}</ErrorText>}
                    <Button type="submit" disabled={loading} style={{ width: "100%", marginTop: 8 }}>
                        {loading ? "Signing in..." : "Sign in"}
                    </Button>
                </form>
            </LoginCard>
        </Wrap>
    );
}