window.downloadFile = function (filename, base64Content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64," + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}