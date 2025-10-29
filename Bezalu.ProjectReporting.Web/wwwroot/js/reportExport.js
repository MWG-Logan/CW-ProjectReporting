window.exportReportPdf = async function(elementId, fileName){
    try {
        const element = document.getElementById(elementId);
        if(!element){
            console.error('Element not found for PDF export', elementId);
            return;
        }
        // Basic print dialog as placeholder. For real PDFs integrate something like jsPDF + html2canvas.
        const printContents = element.innerHTML;
        const win = window.open('', '', 'height=800,width=800');
        win.document.write('<html><head><title>' + fileName + '</title>');
        win.document.write('</head><body >');
        win.document.write(printContents);
        win.document.write('</body></html>');
        win.document.close();
        win.focus();
        win.print();
        win.close();
    } catch(e){
        console.error('PDF export failed', e);
    }
};
