import styled from "styled-components";
import Sidebar from "./Sidebar";
import TopBar from "./TopBar";

const Shell = styled.div`
  height: 100vh;
  display: flex;
`;

const Main = styled.div`
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
`;

const Content = styled.main`
  flex: 1;
  overflow-y: auto;
  padding: 24px 32px;
`;

export default function DashboardLayout({ children }) {
    return (
        <Shell>
            <Sidebar />
            <Main>
                <TopBar />
                <Content>{children}</Content>
            </Main>
        </Shell>
    );
}