import { memo, useEffect, useState } from "react";
import styled from "styled-components";
import { Document as PdfDocument, Page as PdfPage, pdfjs } from "react-pdf";
// Use Vite's asset import to get a URL for the local pdf.worker file from pdfjs-dist.
// The ?url suffix tells Vite to provide the resolved URL as a string.
// Use the ES module worker file shipped with pdfjs-dist (Vite-friendly .mjs)
// The file extension must match what is actually present in node_modules/pdfjs-dist/build
import pdfWorkerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
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
    const [pdfUrl, setPdfUrl] = useState(null);
    const [pdfLoading, setPdfLoading] = useState(false);
    const [textContent, setTextContent] = useState(null);

    const isPdf = document.contentType === "application/pdf" || document.fileName?.endsWith(".pdf");
    const isDocx = document.fileName?.endsWith(".docx");
    const isDoc = document.fileName?.endsWith(".doc") && !isDocx;
    const isText = document.contentType === "text/plain" || document.fileName?.endsWith(".txt");

    useEffect(() => {
        // Configure PDF.js worker. For the installed react-pdf/pdfjs-dist versions
        // the simplest compatible approach is to use the CDN-hosted worker file.
        // This avoids module resolution issues (pdf.worker.mjs) with Vite.
        // Point PDF.js to the local worker file resolved by Vite. This avoids
        // CDN usage and ensures the worker version matches the installed pdfjs-dist.
        try {
            pdfjs.GlobalWorkerOptions.workerSrc = pdfWorkerUrl;
        } catch {
            // ignore if pdfjs is not available for some reason
        }

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
            if (isText) {
                docApi.get(`/documents/${document.id}/download`, { responseType: "text" })
                    .then((res) => {
                        if (!cancelled) setTextContent(res.data);
                    })
                    .catch(() => {
                        if (!cancelled) setTextContent(null);
                    });
            }
            if (isPdf) {
                setPdfLoading(true);
                docApi
                    .get(`/documents/${document.id}/download`, { responseType: "blob" })
                    .then((res) => {
                        if (cancelled) return;
                        const url = URL.createObjectURL(res.data);
                        setPdfUrl(url);
                    })
                    .catch(() => {
                        
                    })
                    .finally(() => {
                        if (!cancelled) setPdfLoading(false);
                    });
            }
        });

        return () => {
            cancelled = true;
            if (pdfUrl) {
                URL.revokeObjectURL(pdfUrl);
            }
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
                        <div>
                            {pdfLoading && <div>Loading PDF preview...</div>}
                            {!pdfLoading && pdfUrl && (
                                <PdfDocument file={pdfUrl} onLoadSuccess={({ numPages }) => setNumPages(numPages)}>
                                    {Array.from({ length: numPages || 0 }, (_, i) => (
                                        <PdfPage key={i} pageNumber={i + 1} width={680} />
                                    ))}
                                </PdfDocument>
                            )}
                            {!pdfLoading && !pdfUrl && <div>Unable to load PDF preview.</div>}
                        </div>
                    )}
                    {isText && textContent && (
                        <pre style={{ whiteSpace: "pre-wrap", fontSize: 13 }}>{textContent}</pre>
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


          