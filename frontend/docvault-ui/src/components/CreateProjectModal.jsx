import { useState } from 'react';
import styled from 'styled-components';
import { docApi } from '../api/api';
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

export default function CreateProjectModal({ onClose, onCreated }) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const handleCreate = async () => {
    setError('');
    if (!name) return setError('Project name is required');
    setBusy(true);
    try {
      await docApi.post('/projects', { name, description });
      onCreated && onCreated();
      onClose && onClose();
    } catch (err) {
      setError(err?.response?.data?.message || err.message || 'Failed to create project');
    } finally { setBusy(false); }
  };

  return (
    <Overlay>
      <Dialog>
        <Close aria-label="Close" onClick={onClose}>×</Close>
        <h3 style={{ textAlign: 'left', marginTop: 0 }}>Create Project</h3>
        <Field>
          <Label>Title</Label>
          <Input placeholder="Enter project title" value={name} onChange={(e)=>setName(e.target.value)} />
        </Field>
        <Field>
          <Label>Description</Label>
          <Input placeholder="Short description" value={description} onChange={(e)=>setDescription(e.target.value)} />
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
