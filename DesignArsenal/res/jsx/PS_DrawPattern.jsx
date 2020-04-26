var code = (function() {
	var idsetd = charIDToTypeID( "setd" );
		var desc127 = new ActionDescriptor();
		var idnull = charIDToTypeID( "null" );
			var ref36 = new ActionReference();
			var idPrpr = charIDToTypeID( "Prpr" );
			var idPtrn = charIDToTypeID( "Ptrn" );
			ref36.putProperty( idPrpr, idPtrn );
			var idcapp = charIDToTypeID( "capp" );
			var idOrdn = charIDToTypeID( "Ordn" );
			var idTrgt = charIDToTypeID( "Trgt" );
			ref36.putEnumerated( idcapp, idOrdn, idTrgt );
		desc127.putReference( idnull, ref36 );
		var idT = charIDToTypeID( "T   " );
		desc127.putPath(idT, new File( "{0}" ) );
		var idAppe = charIDToTypeID( "Appe" );
		desc127.putBoolean( idAppe, true );
	executeAction( idsetd, desc127, DialogModes.NO );

	var idMk = charIDToTypeID( "Mk  " );
		var desc100 = new ActionDescriptor();
		var idnull = charIDToTypeID( "null" );
			var ref30 = new ActionReference();
			var idcontentLayer = stringIDToTypeID( "contentLayer" );
			ref30.putClass( idcontentLayer );
		desc100.putReference( idnull, ref30 );
		var idUsng = charIDToTypeID( "Usng" );
			var desc101 = new ActionDescriptor();
			var idType = charIDToTypeID( "Type" );
				var desc102 = new ActionDescriptor();
				var idPtrn = charIDToTypeID( "Ptrn" );
					var desc103 = new ActionDescriptor();
					var idNm = charIDToTypeID( "Nm  " );
					desc103.putString( idNm, """{1}""" );
					var idIdnt = charIDToTypeID( "Idnt" );
					desc103.putString( idIdnt, """{2}""" );
				var idPtrn = charIDToTypeID( "Ptrn" );
				desc102.putObject( idPtrn, idPtrn, desc103 );
			var idpatternLayer = stringIDToTypeID( "patternLayer" );
			desc101.putObject( idType, idpatternLayer, desc102 );
			var idShp = charIDToTypeID( "Shp " );
				var desc104 = new ActionDescriptor();
				var idunitValueQuadVersion = stringIDToTypeID( "unitValueQuadVersion" );
				desc104.putInteger( idunitValueQuadVersion, 1 );
				var idTop = charIDToTypeID( "Top " );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idTop, idPxl, 0.000000 );
				var idLeft = charIDToTypeID( "Left" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idLeft, idPxl, 0.000000 );
				var idBtom = charIDToTypeID( "Btom" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idBtom, idPxl, 723.000000 );
				var idRght = charIDToTypeID( "Rght" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idRght, idPxl, 1065.000000 );
				var idtopRight = stringIDToTypeID( "topRight" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idtopRight, idPxl, 0.000000 );
				var idtopLeft = stringIDToTypeID( "topLeft" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idtopLeft, idPxl, 0.000000 );
				var idbottomLeft = stringIDToTypeID( "bottomLeft" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idbottomLeft, idPxl, 0.000000 );
				var idbottomRight = stringIDToTypeID( "bottomRight" );
				var idPxl = charIDToTypeID( "#Pxl" );
				desc104.putUnitDouble( idbottomRight, idPxl, 0.000000 );
			var idRctn = charIDToTypeID( "Rctn" );
			desc101.putObject( idShp, idRctn, desc104 );
			
		var idcontentLayer = stringIDToTypeID( "contentLayer" );
		desc100.putObject( idUsng, idcontentLayer, desc101 );
		var idLyrI = charIDToTypeID( "LyrI" );
		desc100.putInteger( idLyrI, 21 );
	executeAction( idMk, desc100, DialogModes.NO );
}).toSource() + "();"


if(app.documents.length == 0) {
	alert("No active document", "DesignArsenal");
} else {
	app.activeDocument.suspendHistory("DesignArsenal - draw pattern", code);
}