import { useEffect, useState } from 'react';
import styled from 'styled-components';
import { userApi } from '../api/api';
import { Button, Select } from '../styles/shared';

const Overlay = styled.div`
  position: fixed;
  left: 0; right: 0; top: 0; bottom: 0;
  background: rgba(0,0,0,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
`;

const Dialog = styled.div`
  width: 640px;
  background: ${(p) => p.theme.color.surface};
  border: 1px solid ${(p) => p.theme.color.border};
  padding: 16px;
  border-radius: 8px;
`;

const Row = styled.div`
  display:flex; align-items:center; justify-content:space-between; gap:12px; padding:8px 0;
`;

export default function EditUserProjectsModal({ user, onClose, onSaved }) {
    // Derive memberships directly from the user prop to avoid unnecessary state duplication
    // Derive memberships directly from the user prop to avoid unnecessary state duplication
    const memberships = user?.projects || [];
    const [allProjects, setAllProjects] = useState([]);
    const [selectedProject, setSelectedProject] = useState('');
    const [selectedRole, setSelectedRole] = useState('User');
    const [busy, setBusy] = useState(false);
    const [message, setMessage] = useState(null);

    // No longer syncing memberships into state — derive from props instead.

    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const res = await userApi.get('/user-projects');
                if (!mounted) return;
                // res.data likely contains a flat list of user-project rows
                const projects = (res.data || []).map(p => ({ id: p.projectId, name: p.projectName }));
                setAllProjects(projects);
            } catch (err) {
                console.warn('Failed to load all projects', err);
            }
        })();
        return () => { mounted = false; };
    }, []);

    const availableProjects = allProjects.filter(p => !memberships.some(m => m.projectId === p.id));

    const handleAdd = async () => {
        if (!selectedProject) return;
        setBusy(true);
        try {
            // Use admin endpoint to add user to project
            await userApi.put(`/users/${user.id}/change-project`, { RequestedProjectId: selectedProject });
            // If role chosen is ProjectHead, promote in that project
            if (selectedRole === 'ProjectHead') {
                await userApi.put(`/users/${user.id}/assign-project-head`, { ProjectId: selectedProject });
            }
            setMessage({ type: 'success', text: 'User added to project.' });
            // refresh parent and close after short delay so admin sees message
            setTimeout(() => { onSaved && onSaved(); }, 600);
        } catch (err) {
            console.warn('Failed to add user to project', err);
            const text = err?.response?.data?.message || err?.message || 'Failed to add user to project';
            setMessage({ type: 'error', text });
        } finally { setBusy(false); }
    };

    const handleRemove = async (projId) => {
        if (!window.confirm('Remove this user from the project?')) return;
        setBusy(true);
        try {
            await userApi.delete(`/users/${user.id}/projects/${projId}`);
            setMessage({ type: 'success', text: 'User removed from project.' });
            setTimeout(() => { onSaved && onSaved(); }, 600);
        } catch (err) {
            console.warn('Failed to remove user from project', err);
            const text = err?.response?.data?.message || err?.message || 'Failed to remove user from project';
            setMessage({ type: 'error', text });
        } finally { setBusy(false); }
    };

    const handleChangeRole = async (projId, newRole) => {
        setBusy(true);
        try {
            await userApi.put(`/users/${user.id}/projects/${projId}/role`, { Role: newRole });
            setMessage({ type: 'success', text: 'Role updated.' });
            setTimeout(() => { onSaved && onSaved(); }, 600);
        } catch (err) {
            console.warn('Failed to change role', err);
            const text = err?.response?.data?.message || err?.message || 'Failed to change role';
            setMessage({ type: 'error', text });
        } finally { setBusy(false); }
    };

    if (!user) return null;

    const InlineMessage = styled.div`
      padding: 10px 12px;
      border-radius: 6px;
      margin-bottom: 12px;
      background: ${(p) => p.type === 'error' ? '#fce8e6' : '#e6ffed'};
      color: ${(p) => p.type === 'error' ? '#b42318' : '#007a3d'};
      border: 1px solid rgba(0,0,0,0.04);
    `;

    return (
        <Overlay>
            <Dialog>
                <h3>Edit Projects for {user.firstName} {user.lastName}</h3>
                {message && <InlineMessage type={message.type}>{message.text}</InlineMessage>}
                <div style={{ marginTop: 12 }}>
                    {memberships.length === 0 ? <div>No project memberships.</div> : (
                        memberships.map(m => (
                            <Row key={m.projectId}>
                                <div>
                                    <div style={{ fontWeight: 600 }}>{m.projectName}</div>
                                    <div style={{ color: '#666' }}>{m.projectId}</div>
                                </div>
                                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                    <Select value={m.role} onChange={(e) => handleChangeRole(m.projectId, e.target.value)}>
                                        <option value="User">User</option>
                                        <option value="ProjectHead">ProjectHead</option>
                                    </Select>
                                    <Button $variant="danger" onClick={() => handleRemove(m.projectId)} disabled={busy}>Remove</Button>
                                </div>
                            </Row>
                        ))
                    )}
                </div>

                <div style={{ marginTop: 16, borderTop: '1px solid #eee', paddingTop: 12 }}>
                    <div style={{ marginBottom: 8 }}>Add Project</div>
                    <div style={{ display: 'flex', gap: 8 }}>
                        <Select value={selectedProject} onChange={e => setSelectedProject(e.target.value)}>
                            <option value="">Select Project</option>
                            {availableProjects.map(p => (
                                <option key={p.id} value={p.id}>{p.name}</option>
                            ))}
                        </Select>
                        <Select value={selectedRole} onChange={e => setSelectedRole(e.target.value)}>
                            <option value="User">User</option>
                            <option value="ProjectHead">ProjectHead</option>
                        </Select>
                        <Button onClick={handleAdd} disabled={busy || !selectedProject}>Add</Button>
                    </div>
                </div>

                <div style={{ marginTop: 16, display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                    <Button $variant="secondary" onClick={onClose}>Close</Button>
                </div>
            </Dialog>
        </Overlay>
    );
}
