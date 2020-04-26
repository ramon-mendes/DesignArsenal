var bOK = false;
var font;

if(app.documents.length == 0)
	alert("No active document", "Design Arsenal");
else if(app.selection.length == 0)
	alert("Nothing is selected", "Design Arsenal");
else
{
	font = app.textFonts.getByName("{0}");

	var sels = app.selection;
	for(var i = 0; i < sels.length; i++)
		ApplyRecurse(sels[i]);

	if(!bOK)
		alert("No text object was selected to apply the new font", "Design Arsenal");
}

function ApplyRecurse(layer)
{
	if(layer instanceof TextFrame) {
		var text = layer;
		var fontStyle = text.textRange.characterAttributes;
		fontStyle.textFont = font;
		bOK = true;
	} else if(layer instanceof GroupItem) {
		var groups = layer.groupItems;
		if(groups) {
			for(var i = 0; i < groups.length; i++)
				ApplyRecurse(groups[i]);
		}

		var texts = layer.textFrames;
		if(texts) {
			for(var i = 0; i < texts.length; i++)
				ApplyRecurse(texts[i])
		}

		//var symbols = layer.symbolItens;
		//$.writeln(symbols)
	}
}