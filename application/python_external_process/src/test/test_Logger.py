from src.Logger import *
from ddt import ddt, data, unpack
from io import StringIO
import os
import os.path
from shutil import rmtree
import unittest
from unittest.mock import patch

@ddt
class TestLogger(unittest.TestCase):
    @unpack
    @data((True, True, ""), (False, False, "run_log"))
    def test_Constructor(
            self,
            expectedVerboseValue,
            shouldContructWithDefaultFileName,
            logFileName):
        trainingLog = None
        if shouldContructWithDefaultFileName:
            trainingLog = Logger(expectedVerboseValue)
        else:
            trainingLog = Logger(expectedVerboseValue, logFileName)
        
        actualVerboseValue = trainingLog._isVerbose
        self.assertEqual(actualVerboseValue, expectedVerboseValue)
        
        expectedFileName = None
        if shouldContructWithDefaultFileName:
            expectedFileName = "training"
        else:
            expectedFileName = logFileName
        
        actualFileName = trainingLog._fileName
        self.assertEqual(actualFileName, expectedFileName)
        
        expectedLogContent = ""
        actualLogContent = trainingLog._content
        self.assertEqual(actualLogContent, expectedLogContent)

    @unpack
    @data((True, "Peugeot 106 Rallye 1.4 75KM", "Peugeot 106 Rallye 1.4 75KM\n"),
            (False, "Peugeot 106 Rallye 1.4 75KM", ""))
    @patch('src.Logger.datetime')
    def test_Append(
            self,
            isLogVerbose,
            infoToAppend,
            expectedPrintMessage,
            mock_datetime):
        mock_datetime.now.return_value = datetime(1995, 7, 4, 17, 15, 0)
        trainingLog = Logger(isLogVerbose)
        logContentBeforeCall = "Ford Sierra II 2.0 DOHC 125KM\n"
        trainingLog._content = logContentBeforeCall
        
        with patch('sys.stdout', new=StringIO()) as fakeOutput:
            trainingLog.Append(infoToAppend)
            actualPrintMessage = fakeOutput.getvalue()
            self.assertEqual(actualPrintMessage, expectedPrintMessage)
        
        expectedLogContentAfterCall = \
                logContentBeforeCall + "[ 1995-07-04 17:15:00 ] " + \
                infoToAppend + "\n"
        actualLogContentAfterCall = trainingLog._content
        self.assertEqual(actualLogContentAfterCall, expectedLogContentAfterCall)

    @unpack
    @data((True, 1), (True, ""), (True, None), (True, [1, 2]), (True, {"a": "b"}),
            (False, 1), (False, ""), (False, None), (False, [1, 2]), (False, {"a": "b"}))
    def test_Append_InvalidParameters(self, isLogVerbose, invalidInfoParameter):
        trainingLog = Logger(isLogVerbose)
        
        logContentBeforeCall = "Ford Sierra II 2.0 DOHC 125KM\n"
        trainingLog._content = logContentBeforeCall
        
        with patch('sys.stdout', new=StringIO()) as fakeOutput:
            trainingLog.Append(invalidInfoParameter)
            expectedPrintMessage = ""
            actualPrintMessage = fakeOutput.getvalue()
            self.assertEqual(actualPrintMessage, expectedPrintMessage)
        
        logContentAfterCall = trainingLog._content
        self.assertEqual(logContentAfterCall, logContentBeforeCall)

    @patch('src.Logger.datetime')
    def test_createEntryHeader(self, mock_datetime):
        mock_datetime.now.return_value = datetime(1995, 7, 4, 17, 15, 0)
        trainingLog = Logger(False)
        expectedEntryHeader = "[ 1995-07-04 17:15:00 ]"
        actualEntryHeader = trainingLog._createEntryHeader()
        self.assertEqual(actualEntryHeader, expectedEntryHeader)

    def test_Save_OK(self):
        testLocation = "TEST_LOCATION_FOR_LOG_FILE"
        if os.path.isdir(testLocation):
            rmtree(testLocation)
        
        os.mkdir(testLocation)
        self.assertTrue(os.path.isdir(testLocation))
        
        trainingLog = Logger(False)
        trainingLog._content = "Ford Sierra II 2.0 DOHC 125KM"
        trainingLog.Save(testLocation)
        
        pathToLogFile = os.path.join(
                testLocation,
                "{0}.log".format(trainingLog._fileName))
        self.assertTrue(os.path.isfile(pathToLogFile))
        
        with open(pathToLogFile, "r") as logFile:
            expectedLogFileContent = trainingLog._content
            actualLogFileContent = logFile.read()
            self.assertEqual(actualLogFileContent, expectedLogFileContent)
        
        rmtree(testLocation)
        self.assertFalse(os.path.isdir(testLocation))
    
    def test_Save_LocationIsNotDir(self):
        nonDirectoryLocation = "TEST_NON_DIRECTORY_LOCATION.log"
        with open(nonDirectoryLocation, "w") as tempFile:
            tempFile.write("kanapka")
        self.assertTrue(os.path.exists(nonDirectoryLocation))
        self.assertFalse(os.path.isdir(nonDirectoryLocation))
        
        trainingLog = Logger(False)
        trainingLog._content = "Ford Sierra II 2.0 DOHC 125KM"
        trainingLog.Save(nonDirectoryLocation)
        
        pathToLogFile = os.path.os.path.join(
                nonDirectoryLocation,
                "%s.log" % trainingLog._fileName)
        doesLogFileExist = os.path.exists(pathToLogFile)
        self.assertFalse(doesLogFileExist)
        os.remove(nonDirectoryLocation)
        self.assertFalse(os.path.exists(nonDirectoryLocation))
    
    def test_Save_LocationDoesNotExist(self):
        nonExistentLocation = "TEST_NON_EXISTENT_LOCATION"
        self.assertFalse(os.path.exists(nonExistentLocation))
        trainingLog = Logger(False)
        trainingLog._content = "Ford Sierra II 2.0 DOHC 125KM"
        trainingLog.Save(nonExistentLocation)
        
        pathToLogFile = os.path.join(
                nonExistentLocation,
                "%s.log" % trainingLog._fileName)
        doesLogFileExist = os.path.exists(pathToLogFile)
        self.assertFalse(doesLogFileExist)
    
    @data(None, 1, 1.0, [1, 2, 3, 4], {"not" : "string"})
    def test_Save_LocationIsNotString(self, notStringLocation):
        trainingLog = Logger(False)
        trainingLog._content = "Ford Sierra II 2.0 DOHC 125KM"
        trainingLog.Save(notStringLocation)
        
        pathToLogFile = os.path.join(
                str(notStringLocation),
                "%s.log" % trainingLog._fileName)
        doesLogFileExist = os.path.exists(pathToLogFile)
        self.assertFalse(doesLogFileExist)
    
    @data("", "    ", "\n \n \t")
    def test_Save_LogContentIsEmptyOrOnlyWhitespaces(self, logContent):
        testLocation = "TEST_LOCATION_FOR_LOG_FILE"
        if os.path.isdir(testLocation):
            rmtree(testLocation)
        
        os.mkdir(testLocation)
        self.assertTrue(os.path.isdir(testLocation))
        
        trainingLog = Logger(False)
        trainingLog._content = logContent
        trainingLog.Save(testLocation)
        
        pathToLogFile = os.path.join(
                testLocation,
                "{0}.log".format(trainingLog._fileName))
        self.assertFalse(os.path.exists(pathToLogFile))
        
        rmtree(testLocation)
        self.assertFalse(os.path.isdir(testLocation))
