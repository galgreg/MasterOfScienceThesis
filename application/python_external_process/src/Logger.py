import os.path
from datetime import datetime

class Logger:
    def __init__(self, isVerbose, fileName = "training"):
        self._isVerbose = isVerbose
        self._fileName = fileName
        self._content = ""

    def Append(self, entryContent):
        if type(entryContent) != str or entryContent == "":
            return
        entryHeader = self._createEntryHeader()
        fullEntry = "{0} {1}".format(entryHeader, entryContent)
        self._content = self._content + fullEntry + "\n"
        if self._isVerbose:
            print(entryContent)

    def _createEntryHeader(self):
        currentDatetime = datetime.now()
        entryHeader = "[ {0} ]".format(currentDatetime)
        return entryHeader

    def Save(self, location):
        if self._content.strip() == "":
            return
        
        if type(location) != str or location.strip() == "" \
                or not os.path.isdir(location):
            return
        
        filePath = os.path.join(location, "{0}.log".format(self._fileName))
        with open(filePath, "w") as logFile:
            logFile.write(self._content)
