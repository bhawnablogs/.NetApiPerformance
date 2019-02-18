using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;

namespace ParserPerformance
{
    public sealed class IO
    {
        private bool _HandleVisaExceptions = false;
        private bool liveConnection;
        private string instAddress;
        private bool mAutoFlush = true;
        private int visaDefaultHandle;
        private int visaHandle = 0;
        private static readonly object commsLock = new Object();
        private int _Timeout = 0;
        private string lastCmd = "start";
        private bool _SendEnd = true;
        private bool _TerminationCharacterEnabled = false;
        private bool _Locked = false;

        internal enum RWMode
        {
            Read = 0,
            Write
        }

        internal bool LiveConnection
        {
            get
            {
                return liveConnection;
            }
        }

        internal bool AutoFlush
        {
            get
            {
                return mAutoFlush;
            }
            set
            {
                mAutoFlush = value;
            }
        }

        internal string Address
        {
            get { return instAddress; }

        }

        internal int Timeout
        {
            get { return _Timeout; }
            set
            {
                int status = visa32.viSetAttribute(visaHandle, visa32.VI_ATTR_TMO_VALUE, value);
                if (status < visa32.VI_SUCCESS)
                {
                    ThrowVisaException(status);
                }

                _Timeout = value;
            }
        }

        internal bool SendEnd
        {
            get { return _SendEnd; }

            set
            {
                //Only change the attribute if we need to
                if (value != _SendEnd)
                {
                    if (visaHandle != 0)
                    {
                        int status = 0;

                        if (value)
                            status = visa32.viSetAttribute(visaHandle, visa32.VI_ATTR_SEND_END_EN, visa32.VI_TRUE);
                        else
                            status = visa32.viSetAttribute(visaHandle, visa32.VI_ATTR_SEND_END_EN, visa32.VI_FALSE);

                        if (status < visa32.VI_SUCCESS)
                        {
                            ThrowVisaException(status);
                        }

                        _SendEnd = value;
                    }
                }
            }
        }

        internal bool TerminationCharacterEnabled
        {
            get { return _TerminationCharacterEnabled; }

            set
            {
                //Only change the attribute if we need to
                if (value != _TerminationCharacterEnabled)
                {
                    if (visaHandle != 0)
                    {
                        int status = 0;

                        if (value)
                            status = visa32.viSetAttribute(visaHandle, visa32.VI_ATTR_TERMCHAR_EN, visa32.VI_TRUE);
                        else
                            status = visa32.viSetAttribute(visaHandle, visa32.VI_ATTR_TERMCHAR_EN, visa32.VI_FALSE);

                        if (status < visa32.VI_SUCCESS)
                        {
                            ThrowVisaException(status);
                        }

                        _TerminationCharacterEnabled = value;
                    }
                }
            }
        }

        internal bool HandleVisaExceptions
        {
            get { return _HandleVisaExceptions; }
            set { _HandleVisaExceptions = value; }
        }

        internal bool Locked
        {
            get
            {
                return _Locked;
            }

            set
            {
                if (visaHandle != 0)
                {
                    if (value == true && !_Locked)
                    {
                        int status = visa32.viLock(visaHandle, visa32.VI_EXCLUSIVE_LOCK, visa32.VI_TMO_IMMEDIATE, "0", new StringBuilder(visa32.VI_NULL));

                        if (status < visa32.VI_SUCCESS)
                        {
                            ThrowVisaException(status);
                        }

                        _Locked = true;
                    }
                    else if (value == false && _Locked)
                    {
                        int status = visa32.viUnlock(visaHandle);
                        if (status < visa32.VI_SUCCESS)
                        {
                            ThrowVisaException(status);
                        }
                        _Locked = false;
                    }
                }
            }
        }

        private void ThrowVisaException(int statusCode)
        {
            string methodName = new StackTrace().GetFrame(1).GetMethod().Name;
            string message = "A VISA error occurred in " + methodName + " (Error " + statusCode + ")";

            VisaStatus vs = (VisaStatus)Enum.Parse(typeof(VisaStatus), statusCode.ToString(), true);

            switch (vs)
            {
                case VisaStatus.ErrorConnectionLost:
                    message = "The IO connection to the instrument has been lost";
                    break;

                case VisaStatus.ErrorIO:
                    message = "An IO error occurred";
                    break;

                case VisaStatus.ErrorTimeout:
                    message = "The IO operation timed out";
                    break;

                case VisaStatus.ErrorResourceLocked:
                    message = "The instrument session is locked";
                    break;

                case VisaStatus.ErrorResourceBusy:
                    message = "The instrument session is busy";
                    break;

                case VisaStatus.ErrorInvalidObject:
                    message = "There is no connection to the instrument";
                    // convert the VI_ERROR_INV_OBJECT to ErrorConnectionLost
                    vs = VisaStatus.ErrorConnectionLost;
                    break;

                default:
                    message = "A VISA error occurred";
                    break;
            }

            message += " (Error " + statusCode + ")";

            throw new VisaException(vs, message);
        }

        private void ExceptionHandler(VisaException ve, RWMode rw)
        {
            if (rw == RWMode.Read)
                Console.Write("LOST READ ");
            else
                Console.Write("LOST WRITE ");

            Console.WriteLine("Last command: " + lastCmd);
            Console.WriteLine("Visa Exception type " + ve.ErrorCode.ToString());

            if ((ve.ErrorCode == VisaStatus.ErrorSystemError ||
                    ve.ErrorCode == VisaStatus.ErrorTimeout ||
                    ve.ErrorCode == VisaStatus.ErrorConnectionLost ||
                    ve.ErrorCode == VisaStatus.ErrorIO ||
                    ve.ErrorCode == VisaStatus.ErrorNotControllerInChange) && rw == RWMode.Write)
            {
                // something major has happened to cause a Write to timeout

                // If writes are timing out, we don't want to waste time trying to close the connection down cleanly,
                // because that will probably timeout too.  Instead, just set the instrument object to null.
                // Close ( ); 

                if (visaHandle != 0)
                {
                    visaHandle = 0;
                    liveConnection = false;
                    // since we've lost the connection to the instrument, rethrow the exception with an explanation message
                    // and tell them we've lost the connection.
                    VisaException writeTimeoutException = new VisaException(VisaStatus.ErrorConnectionLost, "The connection to " + this.Address + " has been lost.", ve);
                    throw writeTimeoutException;
                }
            }
            else if (ve.ErrorCode == VisaStatus.ErrorConnectionLost || ve.ErrorCode == VisaStatus.ErrorIO || ve.ErrorCode == VisaStatus.ErrorSystemError)
            {
                if (visaHandle != 0)
                {
                    // this should prevent every thread from getting hung with Visa Timeouts
                    Close();
                    visaHandle = 0;
                    liveConnection = false;
                    throw ve;
                }
            }

            // else if ( ve.ErrorCode = VisaStatus.ErrorTimeout && rw == RWMode.Read )
            // timed out on a read - may be bacause the query data wasn't available
            // or that something is up with the visa connection.
            // Assume its not a catastrophic event and try to continue on

            if (!HandleVisaExceptions)
            {
                throw ve;
            }
        }

        private void ExceptionHandler(System.Runtime.InteropServices.COMException ce, RWMode rw)
        {
            // this is only called from the Read() call if a COM exception is raised - assume it is not catastrophic and move on
            if (rw == RWMode.Read)
                Console.Write("LOST COM READ ");
            else
                Console.Write("LOST COM WRITE ");

            Console.WriteLine("Last command: " + lastCmd);
            Console.WriteLine("COM Exception type " + ce.ErrorCode.ToString(CultureInfo.InvariantCulture));

            {
                // else if ( ve.ErrorCode = VisaStatus.ErrorTimeout && rw == RWMode.Read )
                // timed out on a read - may be bacause the query data wasn't available
                // or that something is up with the visa connection.
                // Assume its not a catastrophic event and try to continue on

                if (!HandleVisaExceptions)
                {
                    throw ce;
                }
            }
        }

        public void Write(string s)
        {
            //Store VI_ATTR_SEND_END_EN attribute
            bool mLastSendEnd = this.SendEnd;

            if (liveConnection)
            {
                try
                {
                    lock (commsLock)
                    {
                        int bytesWritten = 0;

                        //If Autoflush is false, assume we need to send more data before terminating read
                        //so disable VI_ATTR_SEND_END_EN attribute
                        if (mAutoFlush)
                        {
                            this.SendEnd = true;
                        }
                        else
                        {
                            this.SendEnd = false;
                        }

                        int status = visa32.viWrite(visaHandle, new ASCIIEncoding().GetBytes(s), s.Length, out bytesWritten);


                        if (status < visa32.VI_SUCCESS || s.Length != bytesWritten)
                        {
                            ThrowVisaException(status);
                        }
                    }

                    //If autoflush set then send an sendEnd and flush the buffer - default behaviour,
                    //not convinced that the sendEnd is actually sent on a flush using visa32
                    if (mAutoFlush)
                    {
                        this.FlushWrite(true);
                    }

                }
                catch (VisaException ve)
                {
                    lastCmd = s;
                    ExceptionHandler(ve, RWMode.Write);
                }
                finally
                {
                    //Restore VI_ATTR_SEND_END_EN attribute
                    this.SendEnd = mLastSendEnd;
                }
            }
        }

        // Read() should be visible to customers
        public string Read()
        {
            if (liveConnection)
            {
                try
                {
                    lock (commsLock)
                    {
                        //TODO : Needs to decide Max Length
                        const int MAX_READ_LEN = 2100000;
                        byte[] buffer = new byte[MAX_READ_LEN];
                        string[] readString = new string[MAX_READ_LEN]; // will never use this amount, but just in case
                        int readStrIndex = 0;
                        int bytesRead = 0;

                        int status = visa32.viRead(visaHandle, buffer, MAX_READ_LEN, out bytesRead);
                        while (status == visa32.VI_SUCCESS_MAX_CNT)
                        {
                            readString[readStrIndex] = new ASCIIEncoding().GetString(buffer);
                            buffer = new byte[MAX_READ_LEN];
                            status = visa32.viRead(visaHandle, buffer, MAX_READ_LEN, out bytesRead);
                            readStrIndex++;
                        }

                        if (status < 0)
                        {
                            ThrowVisaException(status);
                        }
                        if (mAutoFlush)
                        {
                            visa32.viFlush(visaHandle, visa32.VI_READ_BUF);
                        }

                        readString[readStrIndex] = new ASCIIEncoding().GetString(buffer);
                        string temp = "";
                        for (int i = 0; i <= readStrIndex; i++)
                        {
                            temp += readString[i];
                        }

                        return temp.ToString().Trim(new char[] { '\n', '\r', '\0' });
                    }
                }
                catch (System.Runtime.InteropServices.COMException ce)
                {
                    ExceptionHandler(ce, RWMode.Read);
                    return "0";
                }
                catch (VisaException ve)
                {
                    ExceptionHandler(ve, RWMode.Read);
                    return "0";
                }
            }
            else
            {
                if (HandleVisaExceptions)
                {
                    return "0";
                }
                else
                {
                    throw new VisaException(VisaStatus.ErrorConnectionLost, "There is no connection to the instrument");
                }
            }

        }

        private static string GetInstrumentResourceName(string Address)
        {
            StringBuilder aliasB = new StringBuilder(1024);
            StringBuilder classB = new StringBuilder(1024);
            StringBuilder resNameB = new StringBuilder(1024);
            short fType = 0, fNum = 0;

            int def_session = 0;

            visa32.viOpenDefaultRM(out def_session);
            visa32.viParseRsrcEx(def_session, Address, ref fType, ref fNum, classB, resNameB, aliasB);
            visa32.viClose(def_session);

            return resNameB.ToString();
        }

        public void Open(string address)
        {
            instAddress = address;

            // Creating the session
            try
            {
                visa32.viOpenDefaultRM(out visaDefaultHandle);
                int status = visa32.viOpen(visaDefaultHandle, address, visa32.VI_NO_LOCK, visa32.VI_TMO_IMMEDIATE, out visaHandle);

                if (status == visa32.VI_SUCCESS)
                {
                    liveConnection = true;
                }
                else
                {
                    // something went wrong, abort
                    visa32.viClose(visaDefaultHandle);
                    ThrowVisaException(status);
                }

                //TODO: Need to set dynamic Timeout
                this.Timeout = 100000; // Set the IO timeout
                visa32.viSetBuf(visaHandle, visa32.VI_READ_BUF, 30000);
                liveConnection = true;

                //SOCKET and COM commections require the Termination Character to be enabled
                if ((GetInstrumentResourceName(address).ToUpper().Contains("SOCKET")) ||
                (GetInstrumentResourceName(address).ToUpper().Contains("ASRL")))
                {
                    this.TerminationCharacterEnabled = true;
                }

            }
            catch (VisaException ve)
            {
                if (!HandleVisaExceptions)
                {
                    throw ve;
                }

                if (ve.ErrorCode == VisaStatus.ErrorResourceNotFound)
                {
                    instAddress = "Visa Resource not found";
                    Console.WriteLine("Failed to open instrument connection, resource not found.");
                }
                else
                {
                    Console.WriteLine("Failed to open instrument connection, unknown reason");
                    instAddress = "Visa Exception";
                }

                Console.WriteLine("Exception details {0}", ve.ToString());
                liveConnection = false;
            }
            catch (System.ArgumentException ae)
            {
                // Handle other errors (invalid VISA resource strings etc) by 
                // saying we're not connected to the instrument.
                liveConnection = false;

                if (!HandleVisaExceptions)
                {
                    throw ae;
                }
            }
        }

        public void Close()
        {
            if (liveConnection)
            {
                liveConnection = false;

                string address = this.Address;

                if (Locked)
                {
                    // if the session was locked, unlock it
                    Locked = false;
                }

                try
                {
                    lock (commsLock)
                    {
                        int status = visa32.viClose(visaHandle);
                        status = visa32.viClose(visaDefaultHandle);
                    }
                }
                catch (VisaException ve)
                {
                    // Nothing really to do if we fail to close the instrument
                    Console.WriteLine(ve.ToString());
                    Console.WriteLine("Visa Exception " + ve.ToString());
                }
            }
        }

        public void FlushWrite()
        {
            bool mLastSendEnd = this.SendEnd;

            if (liveConnection)
            {
                lock (commsLock)
                {
                    this.SendEnd = true;
                    int status = visa32.viFlush(visaHandle, visa32.VI_WRITE_BUF);
                    this.SendEnd = mLastSendEnd;

                    if (status < visa32.VI_SUCCESS)
                    {
                        ThrowVisaException(status);
                    }
                }
            }
        }

        public void FlushWrite(bool mSendEnd)
        {
            bool mLastSendEnd = this.SendEnd;

            if (liveConnection)
            {
                lock (commsLock)
                {
                    this.SendEnd = mSendEnd;
                    int status = visa32.viFlush(visaHandle, visa32.VI_WRITE_BUF);
                    this.SendEnd = mLastSendEnd;

                    if (status < visa32.VI_SUCCESS)
                    {
                        ThrowVisaException(status);
                    }
                }
            }
        }

        public void FlushRead()
        {

            if (liveConnection)
            {
                lock (commsLock)
                {
                    int status = visa32.viFlush(visaHandle, visa32.VI_READ_BUF);

                    if (status < visa32.VI_SUCCESS)
                    {
                        ThrowVisaException(status);
                    }
                }
            }
        }
    }

    public enum VisaStatus
    {
        Success = visa32.VI_SUCCESS,
        ErrorSystemError = visa32.VI_ERROR_SYSTEM_ERROR,
        ErrorInvalidObject = visa32.VI_ERROR_INV_OBJECT,
        ErrorResourceLocked = visa32.VI_ERROR_RSRC_LOCKED,
        ErrorInvalidExpression = visa32.VI_ERROR_INV_EXPR,
        ErrorResourceNotFound = visa32.VI_ERROR_RSRC_NFOUND,
        ErrorInvalidResourceName = visa32.VI_ERROR_INV_RSRC_NAME,
        ErrorInvalidAccessMode = visa32.VI_ERROR_INV_ACC_MODE,
        ErrorTimeout = visa32.VI_ERROR_TMO,
        ErrorClosingFailed = visa32.VI_ERROR_CLOSING_FAILED,
        ErrorInvalidDegree = visa32.VI_ERROR_INV_DEGREE,
        ErrorInvalidJobID = visa32.VI_ERROR_INV_JOB_ID,
        ErrorAttributeNotSupported = visa32.VI_ERROR_NSUP_ATTR,
        ErrorAttributeStateNotSupported = visa32.VI_ERROR_NSUP_ATTR_STATE,
        ErrorAttributeReadOnly = visa32.VI_ERROR_ATTR_READONLY,
        ErrorInvalidLockType = visa32.VI_ERROR_INV_LOCK_TYPE,
        ErrorInvalidAccessKey = visa32.VI_ERROR_INV_ACCESS_KEY,
        ErrorInvalidEvent = visa32.VI_ERROR_INV_EVENT,
        ErrorInvalidMechanism = visa32.VI_ERROR_INV_MECH,
        ErrorHandlerNotInstalled = visa32.VI_ERROR_HNDLR_NINSTALLED,
        ErrorInvalidHandlerReference = visa32.VI_ERROR_INV_HNDLR_REF,
        ErrorInvalidContext = visa32.VI_ERROR_INV_CONTEXT,
        ErrorNotEnabled = visa32.VI_ERROR_NENABLED,
        ErrorAbort = visa32.VI_ERROR_ABORT,
        ErrorRawWriteProtocolViolation = visa32.VI_ERROR_RAW_WR_PROT_VIOL,
        ErrorRawReadProtocolViolation = visa32.VI_ERROR_RAW_RD_PROT_VIOL,
        ErrorOutputProtocolViolation = visa32.VI_ERROR_OUTP_PROT_VIOL,
        ErrorInputProtocolViolation = visa32.VI_ERROR_INP_PROT_VIOL,
        ErrorBusError = visa32.VI_ERROR_BERR,
        ErrorOperationInProgress = visa32.VI_ERROR_IN_PROGRESS,
        ErrorInvalidSetup = visa32.VI_ERROR_INV_SETUP,
        ErrorQueueError = visa32.VI_ERROR_QUEUE_ERROR,
        ErrorAllocationError = visa32.VI_ERROR_ALLOC,
        ErrorInvalidMask = visa32.VI_ERROR_INV_MASK,
        ErrorIO = visa32.VI_ERROR_IO,
        ErrorInvalidFormat = visa32.VI_ERROR_INV_FMT,
        ErrorFormatNotSupported = visa32.VI_ERROR_NSUP_FMT,
        ErrorLineInUse = visa32.VI_ERROR_LINE_IN_USE,
        ErrorModeNotSupported = visa32.VI_ERROR_NSUP_MODE,
        ErrorSrqNotOccurred = visa32.VI_ERROR_SRQ_NOCCURRED,
        ErrorInvalidAddressSpace = visa32.VI_ERROR_INV_SPACE,
        ErrorInvalidOffset = visa32.VI_ERROR_INV_OFFSET,
        ErrorInvalidWidth = visa32.VI_ERROR_INV_WIDTH,
        ErrorOffsetNotSupported = visa32.VI_ERROR_NSUP_OFFSET,
        ErrorVariableWidthNotSupported = visa32.VI_ERROR_NSUP_VAR_WIDTH,
        ErrorWindowNotMapped = visa32.VI_ERROR_WINDOW_NMAPPED,
        ErrorResponsePending = visa32.VI_ERROR_RESP_PENDING,
        ErrorNoListeners = visa32.VI_ERROR_NLISTENERS,
        ErrorNotControllerInChange = visa32.VI_ERROR_NCIC,
        ErrorNotSystemController = visa32.VI_ERROR_NSYS_CNTLR,
        ErrorOperationNotSupported = visa32.VI_ERROR_NSUP_OPER,
        ErrorInterruptPending = visa32.VI_ERROR_INTR_PENDING,
        ErrorAsrlParity = visa32.VI_ERROR_ASRL_PARITY,
        ErrorAsrlFraming = visa32.VI_ERROR_ASRL_FRAMING,
        ErrorAsrlOverrun = visa32.VI_ERROR_ASRL_OVERRUN,
        ErrorTrigNotMapped = visa32.VI_ERROR_TRIG_NMAPPED,
        ErrorAlignmentOffsetNotSupported = visa32.VI_ERROR_NSUP_ALIGN_OFFSET,
        ErrorUserBuffer = visa32.VI_ERROR_USER_BUF,
        ErrorResourceBusy = visa32.VI_ERROR_RSRC_BUSY,
        ErrorWidthNotSupported = visa32.VI_ERROR_NSUP_WIDTH,
        ErrorInvalidParameter = visa32.VI_ERROR_INV_PARAMETER,
        ErrorInvalidProtocol = visa32.VI_ERROR_INV_PROT,
        ErrorInvalidSize = visa32.VI_ERROR_INV_SIZE,
        ErrorWindowMapped = visa32.VI_ERROR_WINDOW_MAPPED,
        ErrorOperationNotImplemented = visa32.VI_ERROR_NIMPL_OPER,
        ErrorInvalidLength = visa32.VI_ERROR_INV_LENGTH,
        ErrorInvalidMode = visa32.VI_ERROR_INV_MODE,
        ErrorSessionNotLocked = visa32.VI_ERROR_SESN_NLOCKED,
        ErrorMemoryNotShared = visa32.VI_ERROR_MEM_NSHARED,
        ErrorLibraryNotFound = visa32.VI_ERROR_LIBRARY_NFOUND,
        ErrorInterruptNotSupported = visa32.VI_ERROR_NSUP_INTR,
        ErrorInvalidLine = visa32.VI_ERROR_INV_LINE,
        ErrorFileAccess = visa32.VI_ERROR_FILE_ACCESS,
        ErrorFileIO = visa32.VI_ERROR_FILE_IO,
        ErrorLineNotSupported = visa32.VI_ERROR_NSUP_LINE,
        ErrorMechanismNotSupported = visa32.VI_ERROR_NSUP_MECH,
        ErrorInterfaceNumberNotConfigured = visa32.VI_ERROR_INTF_NUM_NCONFIG,
        ErrorConnectionLost = visa32.VI_ERROR_CONN_LOST,
        ErrorDriver = int.MinValue
    }


    public sealed class VisaException : Exception
    {
        private int errorNumber;
        public VisaStatus ErrorCode;
        public int VisaErrorNumber; // = 0

        public VisaException(VisaStatus ErrorCode, string Message)
            : base(Message)
        {
            this.ErrorCode = ErrorCode;
        }

        public VisaException(VisaStatus ErrorCode, string Message, Exception InnerException)
            : base(Message, InnerException)
        {
            this.ErrorCode = ErrorCode;
        }

        public VisaException(string Error)
            : base("Error returned from Visa: " + Error)
        {
        }

        public VisaException(string Error, string Value)
            : base(String.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "Error returned from Visa: 0x{0:X8} {1}",
            Int32.Parse(Value, NumberFormatInfo.InvariantInfo), Value))
        {
            errorNumber = (int)(Int32.Parse(Value, NumberFormatInfo.InvariantInfo));
        }


        public VisaException(VisaStatus ErrorCode)
        {
            this.ErrorCode = ErrorCode;
        }
    }
}
