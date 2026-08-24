import { useState, useEffect } from 'react';
import styled from 'styled-components';
import { userApi } from '../api/api';
import { Button, Field, Label, Input } from '../styles/shared';

const Overlay = styled.div`
  position: fixed; left:0; right:0; top:0; bottom:0; background: rgba(0,0,0,0.3);
  display:flex; align-items:center; justify-content:center; z-index:9999;
`;
const Dialog = styled.div`
  width: 500px; max-width: 95%; background: ${(p) => p.theme.color.surface}; border:1px solid ${(p) => p.theme.color.border}; padding:16px; border-radius:8px; position:relative;
  text-align: left;
`;
const Close = styled.button`
  position:absolute; right:8px; top:8px; border:none; background:transparent; font-size:18px; cursor:pointer;
`;

export default function CreateUserModal({ onClose, onCreated }) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [allProjects, setAllProjects] = useState([]);
  const [selectedProjectIds, setSelectedProjectIds] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const res = await userApi.get('/user-projects');
        if (!mounted) return;
        setAllProjects((res.data || []).map(p => ({ id: p.projectId, name: p.projectName })));
      } catch (err) {
        // ignore
      }
    })();
    return () => { mounted = false; };
  }, []);

  const handleCreate = async () => {
    setError('');
    if (!email) return setError('Email is required');
    setBusy(true);
    try {
      await userApi.post('/users', { Email: email, FirstName: firstName, LastName: lastName, Password: password, ProjectIds: selectedProjectIds });
      onCreated && onCreated();
      onClose && onClose();
    } catch (err) {
      setError(err?.response?.data?.message || err.message || 'Failed to create user');
    } finally { setBusy(false); }
  };

  return (
    <Overlay>
      <Dialog>
        <Close aria-label="Close" onClick={onClose}>×</Close>
        <h3 style={{ textAlign: 'left', marginTop: 0 }}>Create User</h3>
        <Field>
          <Label>First name</Label>
          <Input placeholder="First name" value={firstName} onChange={(e)=>setFirstName(e.target.value)} />
        </Field>
        <Field>
          <Label>Last name</Label>
          <Input placeholder="Last name" value={lastName} onChange={(e)=>setLastName(e.target.value)} />
        </Field>
        <Field>
          <Label>Email</Label>
          <Input placeholder="email@company.com" value={email} onChange={(e)=>setEmail(e.target.value)} />
        </Field>
        <Field>
          <Label>Temporary password</Label>
          <Input placeholder="Temporary password" type="password" value={password} onChange={(e)=>setPassword(e.target.value)} />
        </Field>

        <Field>
          <Label>Projects</Label>
          {allProjects.length === 0 ? (
            <div style={{ color: '#6B778C' }}>No projects</div>
          ) : (
            <select multiple value={selectedProjectIds} onChange={(e) => setSelectedProjectIds(Array.from(e.target.selectedOptions).map(o => o.value))} style={{ minWidth: 220 }}>
              {allProjects.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          )}
        </Field>
        {error && <div style={{ color: '#BF2600', marginBottom: 8 }}>{error}</div>}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button $variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={handleCreate} disabled={busy}>{busy ? 'Creating...' : 'Create'}</Button>
        </div>
      </Dialog>
    </Overlay>
  );
}
