var Testlib = Testlib||{};
Testlib.localization = Testlib.localization||{};
Testlib.localization.Resource = (function () { 
	var strings = {
  "String1": "test 34",
  "String2": "test2"
};
	return $.extend({}, Testlib.localization.Resource || {}, strings);
}());