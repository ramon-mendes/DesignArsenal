var psfont = '{0}';
var found = false;

function GetScriptFolder() {
	var script_fullpath = $.fileName;
	var script_folder = Folder(script_fullpath).path;
	return script_folder + "/";
}

function WriteFile(path, content) {
	var f = new File(path);
	f.encoding = "UTF8";
	f.open("w");
	f.write(content);
	f.close();
}


if(app.documents.length==0)
{
	WriteFile(GetScriptFolder() + 'res.json', 'nodoc');
	alert("No active document", "DesignArsenal");
} else {
	if(BridgeTalk.appName == "photoshop")
	{
		var resfont;
		var code = (function() {
			var doc = app.activeDocument;
			var isset = false;
			try { isset = doc.activeLayer instanceof LayerSet; }
			catch(err) { }

			var set = isset ? doc.activeLayer : doc.activeLayer.parent;
			var layer = set ? set.artLayers.add() : doc.artLayers.add();
			layer.kind = LayerKind.TEXT;

			var textItem = layer.textItem;
			textItem.contents = 'MEOW';
			textItem.font = psfont;
			resfont = textItem.font;
			layer.remove();
		}).toSource() + "();"

		app.activeDocument.suspendHistory("DesignArsenal refresh layers", code);
		found = resfont == psfont;
	}
	else if(BridgeTalk.appName == "illustrator")
	{
		var text = app.activeDocument.textFrames.pointText([0, 0]);
		text.remove();
	
		try {
			app.textFonts.getByName(psfont);
			found = true;
		} catch(e) {
		}
	}
	
	WriteFile(GetScriptFolder() + 'res.json', found);
}