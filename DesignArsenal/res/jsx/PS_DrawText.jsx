var code = (function() {
	var doc = app.activeDocument;
	var isset = false;
	try { isset = doc.activeLayer instanceof LayerSet; }
	catch(err) { }

	var set = isset ? doc.activeLayer : doc.activeLayer.parent;
	var layer = set ? set.artLayers.add() : doc.artLayers.add();
	layer.kind = LayerKind.TEXT;

	var textItem = layer.textItem;
	textItem.contents = "{0}";
	textItem.size = {1};
	textItem.font = "{2}";


	/*var clr = new SolidColor();
	clr.rgb.hexValue = "{0}";
	textItem.color = clr;*/

	var bounds = layer.boundsNoEffects;
	if(!bounds)
	{
		bounds = layer.bounds;
	}

	var layer_width = (bounds[2] - bounds[0]).value;
	var layer_height = (bounds[3] - bounds[1]).value;

	var center_x = doc.width / 2;
	var center_y = doc.height / 2;
	var layer_x = center_x - layer_width / 2;
	var layer_y = center_y - layer_height / 2;

	var tx = layer_x - bounds[0].value;
	var ty = layer_y - bounds[1].value;
	layer.translate(tx, ty);
}).toSource() + "();";

if(app.documents.length==0)
	alert("No active document", "DesignArsenal");
else
	app.activeDocument.suspendHistory("DesignArsenal - add text", code);