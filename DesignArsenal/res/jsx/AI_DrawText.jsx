function writeFile(path, content) {
	var f = new File(path);
	f.encoding = "UTF8";
	f.open("w");
	f.write(content);
	f.close();
}

if(app.documents.length == 0)
{
	alert("No active document", "Design Arsenal")
} else {
	try {
		app.textFonts.getByName('{2}');
	} catch (e) {
	}

	try
	{
		var font = app.textFonts.getByName('{2}');
		var center = app.activeDocument.activeView.centerPoint
		var text = app.activeDocument.textFrames.pointText(center);
		text.contents = '{1}';

		var fontStyle = text.textRange.characterAttributes;
		fontStyle.textFont = font;
		fontStyle.size = {3};

		app.activeDocument.selection = text;
	} catch(e) {
		alert("DesignArsenal: plz, try again", "Design Arsenal");
	}
}