"""
Copyright (c) 2012 Fredrik Ehnbom

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
"""
import sublime_plugin
import os.path
import re
from sublimecompletioncommon import completioncommon


class CompleteSharpDotComplete(completioncommon.CompletionCommonDotComplete):
    pass


class CompleteSharpCompletion(completioncommon.CompletionCommon):
    def __init__(self):
        super(CompleteSharpCompletion, self).__init__("CompleteSharp.sublime-settings", os.path.dirname(os.path.abspath(__file__)))

    def get_packages(self, data):
        packages = re.findall("[ \t]*using[ \t]+(.*);", data)
        packages.append("System")
        packages.append("")
        return packages

    def get_cmd(self):
        return "mono CompleteSharp.exe"

    def is_supported_language(self, view):
        if view.is_scratch():
            return False
        language = self.get_language(view)
        return language == "cs"

comp = CompleteSharpCompletion()


class CompleteSharp(sublime_plugin.EventListener):

    def on_query_completions(self, view, prefix, locations):
        ret = comp.on_query_completions(view, prefix, locations)
        print "ret: %s" % ret
        return ret

    def on_query_context(self, view, key, operator, operand, match_all):
        if key == "completesharp.dotcomplete":
            return comp.get_setting(key.replace(".", "_"), True)
        elif key == "completesharp.supported_language":
            return comp.is_supported_language(view)
        else:
            return comp.on_query_context(view, key, operator, operand, match_all)
