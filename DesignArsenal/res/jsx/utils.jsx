// $.writeln ends opening ESTK

function Execute(path)
{
    var f = new File(path);
    return f.execute();
}

function ExecuteFontMissDialog(app)
{
	return Execute($.fd_dir + "RunMissingDlg" + app + ".exe");
}


function writeFile(path, content) {
	var f = new File(path);
	f.encoding = "UTF8";
	f.open("w");
	f.write(content);
	f.close();
}

function GetScriptFolder()
{
	// Get the fullpath of the script
	var script_fullpath = $.fileName;
	// Only get the folder path
	var script_folder = Folder(script_fullpath).path;
	return script_folder;
}

var console = {
	time: function() {
		$.hiresTimer;// A high-resolution timer that measures the number of microseconds since this property was last accessed
	},
	timeEnd: function() {
		var mms = $.hiresTimer/1000;
		//$.writeln("Took: " + mms);
		return mms;
	},
	clear: function() {
		var bt = new BridgeTalk();
		bt.target = 'estoolkit';
		bt.body = function(){
			app.clc();
		}.toSource()+"()";
		bt.send(5);
	}
};

$.setTimeout = function(func, time) {// DONT USE IT, IT BLOCKS
	while(true)
	{
		$.sleep(time);
        func();
	}
};




/*function getInternalFontName(pFontName) {
	for(var i = 0; i < app.fonts.length; i++) {
		if(pFontName == app.fonts[i].postScriptName)
			return pFontName; // already is an internal font name.
		if(pFontName == app.fonts[i].name)
			return app.fonts[i].postScriptName; // found an internal name.
	}
	throw new Error("Not found");
	return null;
}

function getFontByPSName(internalName) {
	for(var i = 0; i < app.fonts.length; i++) {
		if(internalName == app.fonts[i].postScriptName)
			return app.fonts[i].name;
	}
	return null;// font not installed
}*/