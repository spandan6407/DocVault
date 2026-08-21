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
                {/*this is whatever we are using inside the dashboard... like we take the use of the dashboard inside the admin page...the sidebar and the top bar will be there and rest what we add inside the dashboard layout that will ve different for each of the page  */}
                <Content>{children}</Content>
            </Main>
        </Shell>
    );
}