var cTID = function (s) { return app.charIDToTypeID(s); };
var sTID = function (s) { return app.stringIDToTypeID(s); };

function newGroupFromLayers(doc) {
	var desc = new ActionDescriptor();
	var ref = new ActionReference();
	ref.putClass(sTID('layerSection'));
	desc.putReference(cTID('null'), ref);
	var lref = new ActionReference();
	lref.putEnumerated(cTID('Lyr '), cTID('Ordn'), cTID('Trgt'));
	desc.putReference(cTID('From'), lref);
	executeAction(cTID('Mk  '), desc, DialogModes.NO);
};

function undo() {
	executeAction(cTID("undo", undefined, DialogModes.NO));
};

function getSelectedLayers(doc) {
	var selLayers = [];
	newGroupFromLayers();

	var group = doc.activeLayer;
	var layers = group.layers;

	for(var i = 0; i < layers.length; i++) {
		selLayers.push(layers[i]);
	}

	undo();
	return selLayers;
};


// -----------------------------------------------
var foundTextLayer = false;

function ApplyFont(layer) {
	if(layer.typename == "LayerSet") {
		for(var i = 0; i < layer.layers.length; i++)
			ApplyFont(layer.layers[i]);
	} else if(layer.kind == LayerKind.TEXT) {
		var textItem = layer.textItem;
		textItem.font = "{0}";
		foundTextLayer = true;
	}
}

var code = (function () {
	var docRef = app.activeDocument;
	var selectedLayers = getSelectedLayers(app.activeDocument);

	for(var i = 0; i < selectedLayers.length; i++)
		ApplyFont(selectedLayers[i]);
}).toSource() + "();"

if(app.documents.length == 0) {
	alert("No active document", "Font Drop");
} else {
	app.activeDocument.suspendHistory("FontDrop - apply font", code);
	if(!foundTextLayer)
		alert("No text layer was selected for applying the font", "Font Drop");
}