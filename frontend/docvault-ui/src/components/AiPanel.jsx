import { memo, useCallback, useState } from "react";
import styled from "styled-components";
import { docApi } from "../api/api";
import { Button, Field, Input, Label, SectionTitle } from "../styles/shared";

const Wrap = styled.div`
  border-top: 1px solid ${(p) => p.theme.color.border};
  margin-top: 16px;
  padding-top: 16px;
`;

const AnswerBox = styled.div`
  margin-top: 10px;
  padding: 12px;
  background: ${(p) => p.theme.color.bg};
  border-radius: ${(p) => p.theme.radius};
  font-size: 13px;
  line-height: 1.5;
  white-space: pre-wrap;
`;

const AskRow = styled.form`
  display: flex;
  gap: 8px;
  align-items: flex-end;
`;

function AiPanel({ documentId }) {
    const [summary, setSummary] = useState(null);
    const [summaryLoading, setSummaryLoading] = useState(false);

    const [question, setQuestion] = useState("");
    const [answer, setAnswer] = useState(null);
    const [askLoading, setAskLoading] = useState(false);

    const handleSummarize = useCallback(async () => {
        setSummaryLoading(true);
        try {
            const res = await docApi.get(`/ai/documents/${documentId}/summary`);
            setSummary(res.data.summary);
        } finally {
            setSummaryLoading(false);
        }
    }, [documentId]);

    const handleAsk = useCallback(
        async (e) => {
            e.preventDefault();
            if (!question.trim()) return;
            setAskLoading(true);
            try {
                const res = await docApi.post(`/ai/documents/${documentId}/ask`, { question });
                setAnswer(res.data);
            } finally {
                setAskLoading(false);
            }
        },
        [documentId, question]
    );

    return (
        <Wrap>
            <SectionTitle>Ask the document</SectionTitle>
            <Button type="button" onClick={handleSummarize} disabled={summaryLoading}>
                {summaryLoading ? "Summarizing..." : "Generate Summary"}
            </Button>
            {summary && <AnswerBox>{summary}</AnswerBox>}

            <Field style={{ marginTop: 16 }}>
                <Label htmlFor="ai-question">Question</Label>
                <AskRow onSubmit={handleAsk}>
                    <Input
                        id="ai-question"
                        value={question}
                        onChange={(e) => setQuestion(e.target.value)}
                        placeholder="What does this document say about..."
                    />
                    <Button type="submit" disabled={askLoading}>
                        {askLoading ? "Thinking..." : "Ask"}
                    </Button>
                </AskRow>
            </Field>
            {answer && <AnswerBox>{typeof answer === "string" ? answer : JSON.stringify(answer)}</AnswerBox>}
        </Wrap>
    );
}

export default memo(AiPanel);