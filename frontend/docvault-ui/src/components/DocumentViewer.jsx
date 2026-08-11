import { memo, useEffect, useState } from "react";
import styled from "styled-components";
import { Document as PdfDocument, Page as PdfPage } from "react-pdf";
import mammoth from "mammoth";
import { docApi } from "../api/api";
import AiPanel from "./AiPanel";
import { Button } from "../styles/shared";

const Overlay = styled.div`
  position: fixed;
  inset: 0;
  background: rgba(9, 30, 66, 0.54);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
`;

const Modal = styled.div`
  background: ${(p) => p.theme.color.surface};
  border-radius: ${(p) => p.theme.radius};
  width: min(760px, 92vw);
  max-height: 88vh;
  display: flex;
  flex-direction: column;
`;

const ModalHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 20px;
  border-bottom: 1px solid ${(p) => p.theme.color.border};
`;

const ModalBody = styled.div`
  padding: 20px;
  overflow-y: auto;
`;

const DocxHtml = styled.div`
  font-size: 13px;
  line-height: 1.6;
  img { max-width: 100%; }
`;

function DocumentViewer({ document, onClose }) {
    const [docxHtml, setDocxHtml] = useState(null);
    const [numPages, setNumPages] = useState(null);

    const isPdf = document.contentType === "application/pdf" || document.fileName?.endsWith(".pdf");
    const isDocx = document.fileName?.endsWith(".docx");
    const isDoc = document.fileName?.endsWith(".doc") && !isDocx;

    useEffect(() => {
        let cancelled = false;

        queueMicrotask(() => {
            if (cancelled) return;
            setDocxHtml(null);
            setNumPages(null);

            if (isDocx) {
                docApi.get(`/documents/${document.id}/download`, { responseType: "arraybuffer" }).then(async (res) => {
                    const { value } = await mammoth.convertToHtml({ arrayBuffer: res.data });
                    if (!cancelled) setDocxHtml(value);
                });
            }
        });

        return () => {
            cancelled = true;
        };
    }, [document.id, isDocx]);

    return (
        <Overlay onClick={onClose}>
            <Modal onClick={(e) => e.stopPropagation()}>
                <ModalHeader>
                    <strong>{document.title}</strong>
                    <Button $variant="secondary" onClick={onClose}>
                        Close
                    </Button>
                </ModalHeader>
                <ModalBody>
                    {isPdf && (
                        <PdfDocument
                            file={`/api/documents/${document.id}/download`}
                            onLoadSuccess={({ numPages }) => setNumPages(numPages)}
                        >
                            {Array.from({ length: numPages || 0 }, (_, i) => (
                                <PdfPage key={i} pageNumber={i + 1} width={680} />
                            ))}
                        </PdfDocument>
                    )}
                    {isDocx && docxHtml && <DocxHtml dangerouslySetInnerHTML={{ __html: docxHtml }} />}
                    {isDoc && <p>Legacy .doc files can't be previewed — use Download instead.</p>}

                    <AiPanel documentId={document.id} />
                </ModalBody>
            </Modal>
        </Overlay>
    );
}

export default memo(DocumentViewer);


          