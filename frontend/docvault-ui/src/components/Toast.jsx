import { useEffect } from 'react';
import styled from 'styled-components';

const Wrap = styled.div`
  position: fixed;
  right: 16px;
  bottom: 16px;
  z-index: 1000;
`;

const Notice = styled.div`
  background: ${(p) => p.type === 'error' ? '#fce8e6' : '#e6ffed'};
  color: ${(p) => p.type === 'error' ? '#b42318' : '#007a3d'};
  padding: 12px 16px;
  border-radius: 6px;
  box-shadow: 0 6px 20px rgba(2,6,23,0.2);
  margin-top: 8px;
`;

export default function Toast({ messages, onRemove }) {
    useEffect(() => {
        const timers = messages.map(m => setTimeout(() => onRemove(m.id), 8000));
        return () => timers.forEach(clearTimeout);
    }, [messages, onRemove]);

    if (!messages || messages.length === 0) return null;
    return (
        <Wrap>
            {messages.map(m => (
                <Notice key={m.id} type={m.type}>{m.text}</Notice>
            ))}
        </Wrap>
    );
}
