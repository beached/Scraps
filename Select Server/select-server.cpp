/***********************************************************************
 select-server.cpp - Implements a simple Winsock server that uses 
    select() to handle data from multiple connections within a single
    thread.  All read data is echoed back out to the client.

 Compiling:
    VC++: cl -GX select-server.cpp main.cpp ws-util.cpp wsock32.lib
    BC++: bcc32 select-server.cpp main.cpp ws-util.cpp
    
 This program is hereby released into the public domain.  There is
 ABSOLUTELY NO WARRANTY WHATSOEVER for this product.  Caveat hacker.
***********************************************************************/

#include "ws-util.h"

#include <winsock.h>

#include <iostream>
#include <vector>

using namespace std;


////////////////////////////////////////////////////////////////////////
// Constants 

const int kBufferSize = 1024;
        

////////////////////////////////////////////////////////////////////////
// Globals and types

struct Connection {
    SOCKET sd;
    char acBuffer[kBufferSize];
    int nCharsInBuffer;

    Connection(SOCKET sd_) : sd(sd_), nCharsInBuffer(0) { }
};
typedef vector<Connection> ConnectionList;

ConnectionList gConnections;


////////////////////////////////////////////////////////////////////////
// Prototypes

SOCKET SetUpListener(const char* pcAddress, int nPort);
void AcceptConnections(SOCKET ListeningSocket);


//// DoWinsock /////////////////////////////////////////////////////////
// The module's driver function -- we just call other functions and
// interpret their results.

int DoWinsock(const char* pcAddress, int nPort)
{
    cout << "Establishing the listener..." << endl;
    SOCKET ListeningSocket = SetUpListener(pcAddress, htons(nPort));
    if (ListeningSocket == INVALID_SOCKET) {
        cout << endl << WSAGetLastErrorMessage("establish listener") << 
                endl;
        return 3;
    }

    cout << "Waiting for connections..." << flush;
    while (1) {
        AcceptConnections(ListeningSocket);
        cout << "Acceptor restarting..." << endl;
    }

#if defined(_MSC_VER)
    return 0;   // warning eater
#endif
}


//// SetUpListener /////////////////////////////////////////////////////
// Sets up a listener on the given interface and port, returning the
// listening socket if successful; if not, returns INVALID_SOCKET.

SOCKET SetUpListener(const char* pcAddress, int nPort)
{
    u_long nInterfaceAddr = inet_addr(pcAddress);
    if (nInterfaceAddr != INADDR_NONE) {
        SOCKET sd = socket(AF_INET, SOCK_STREAM, 0);
        if (sd != INVALID_SOCKET) {
            sockaddr_in sinInterface;
            sinInterface.sin_family = AF_INET;
            sinInterface.sin_addr.s_addr = nInterfaceAddr;
            sinInterface.sin_port = nPort;
            if (bind(sd, (sockaddr*)&sinInterface, 
                    sizeof(sockaddr_in)) != SOCKET_ERROR) {
                listen(sd, SOMAXCONN);
                return sd;
            }
            else {
                cerr << WSAGetLastErrorMessage("bind() failed") <<
                        endl;
            }
        }
    }

    return INVALID_SOCKET;
}


//// SetupFDSets ///////////////////////////////////////////////////////
// Set up the three FD sets used with select() with the sockets in the
// connection list.  Also add one for the listener socket, if we have
// one.

void SetupFDSets(fd_set& ReadFDs, fd_set& WriteFDs, 
        fd_set& ExceptFDs, SOCKET ListeningSocket = INVALID_SOCKET) 
{
    FD_ZERO(&ReadFDs);
    FD_ZERO(&WriteFDs);
    FD_ZERO(&ExceptFDs);

    // Add the listener socket to the read and except FD sets, if there
    // is one.
    if (ListeningSocket != INVALID_SOCKET) {
        FD_SET(ListeningSocket, &ReadFDs);
        FD_SET(ListeningSocket, &ExceptFDs);
    }

    // Add client connections
    ConnectionList::iterator it = gConnections.begin();
    while (it != gConnections.end()) {
		if (it->nCharsInBuffer < kBufferSize) {
			// There's space in the read buffer, so pay attention to
			// incoming data.
			FD_SET(it->sd, &ReadFDs);
		}

        if (it->nCharsInBuffer > 0) {
            // There's data still to be sent on this socket, so we need
            // to be signalled when it becomes writable.
            FD_SET(it->sd, &WriteFDs);
        }

        FD_SET(it->sd, &ExceptFDs);

        ++it;
    }
}


//// ReadData //////////////////////////////////////////////////////////
// Data came in on a client socket, so read it into the buffer.  Returns
// false on failure, or when the client closes its half of the
// connection.  (WSAEWOULDBLOCK doesn't count as a failure.)

bool ReadData(Connection& conn) 
{
    int nBytes = recv(conn.sd, conn.acBuffer + conn.nCharsInBuffer,
            kBufferSize - conn.nCharsInBuffer, 0);
    if (nBytes == 0) {
        cout << "Socket " << conn.sd << 
                " was closed by the client. Shutting down." << endl;
        return false;
    }
    else if (nBytes == SOCKET_ERROR) {
        // Something bad happened on the socket.  It could just be a
        // "would block" notification, or it could be something more
        // serious.  Let caller handle the latter case.  WSAEWOULDBLOCK
        // can happen after select() says a socket is readable under
        // Win9x: it doesn't happen on WinNT/2000 or on Unix.
        int err;
        int errlen = sizeof(err);
        getsockopt(conn.sd, SOL_SOCKET, SO_ERROR, (char*)&err, &errlen);
        return (err == WSAEWOULDBLOCK);
    }
    
    // We read some bytes.  Advance the buffer size counter.
    conn.nCharsInBuffer += nBytes;
    
    return true;
}


//// WriteData /////////////////////////////////////////////////////////
// The connection is writable, so send any pending data.  Returns
// false on failure.  (WSAEWOULDBLOCK doesn't count as a failure.)

bool WriteData(Connection& conn) 
{
    int nBytes = send(conn.sd, conn.acBuffer, conn.nCharsInBuffer, 0);
    if (nBytes == SOCKET_ERROR) {
        // Something bad happened on the socket.  Deal with it.
        int err;
        int errlen = sizeof(err);
        getsockopt(conn.sd, SOL_SOCKET, SO_ERROR, (char*)&err, &errlen);
        return (err == WSAEWOULDBLOCK);
    }
    
    if (nBytes == conn.nCharsInBuffer) {
        // Everything got sent, so take a shortcut on clearing buffer.
        conn.nCharsInBuffer = 0;
    }
    else {
        // We sent part of the buffer's data.  Remove that data from
        // the buffer.
        conn.nCharsInBuffer -= nBytes;
        memmove(conn.acBuffer, conn.acBuffer + nBytes, 
                conn.nCharsInBuffer);
    }
    
    return true;
}


//// AcceptConnections /////////////////////////////////////////////////
// Spin forever handling connections.  If something bad happens, we
// return.

void AcceptConnections(SOCKET ListeningSocket)
{
    sockaddr_in sinRemote;
    int nAddrSize = sizeof(sinRemote);

    while (1) {
        fd_set ReadFDs, WriteFDs, ExceptFDs;
        SetupFDSets(ReadFDs, WriteFDs, ExceptFDs, ListeningSocket);

        if (select(0, &ReadFDs, &WriteFDs, &ExceptFDs, 0) > 0) {
            //// Something happened on one of the sockets.
            // Was it the listener socket?...
            if (FD_ISSET(ListeningSocket, &ReadFDs)) {
                SOCKET sd = accept(ListeningSocket, 
                        (sockaddr*)&sinRemote, &nAddrSize);
                if (sd != INVALID_SOCKET) {
                    // Tell user we accepted the socket, and add it to
                    // our connecition list.
                    cout << "Accepted connection from " <<
                            inet_ntoa(sinRemote.sin_addr) << ":" <<
                            ntohs(sinRemote.sin_port) << 
                            ", socket " << sd << "." << endl;
                    gConnections.push_back(Connection(sd));
					if ((gConnections.size() + 1) > 64) {
						// For the background on this check, see
						// www.tangentsoft.net/wskfaq/advanced.html#64sockets
						// The +1 is to account for the listener socket.
						cout << "WARNING: More than 63 client "
								"connections accepted.  This will not "
								"work reliably on some Winsock "
								"stacks!" << endl;
					}

                    // Mark the socket as non-blocking, for safety.
                    u_long nNoBlock = 1;
                    ioctlsocket(sd, FIONBIO, &nNoBlock);
                }
                else {
                    cerr << WSAGetLastErrorMessage("accept() failed") << 
                            endl;
                    return;
                }
            }
            else if (FD_ISSET(ListeningSocket, &ExceptFDs)) {
                int err;
                int errlen = sizeof(err);
                getsockopt(ListeningSocket, SOL_SOCKET, SO_ERROR,
                        (char*)&err, &errlen);
                cerr << WSAGetLastErrorMessage(
                        "Exception on listening socket: ", err) << endl;
                return;
            }

            // ...Or was it one of the client sockets?
            ConnectionList::iterator it = gConnections.begin();
            while (it != gConnections.end()) {
                bool bOK = true;
                const char* pcErrorType = 0;

                // See if this socket's flag is set in any of the FD
                // sets.
                if (FD_ISSET(it->sd, &ExceptFDs)) {
                    bOK = false;
                    pcErrorType = "General socket error";
                    FD_CLR(it->sd, &ExceptFDs);
                }
                else {
                    if (FD_ISSET(it->sd, &ReadFDs)) {
                        cout << "Socket " << it->sd << 
                                " became readable; handling it." << 
                                endl;
                        bOK = ReadData(*it);
                        pcErrorType = "Read error";
                        FD_CLR(it->sd, &ReadFDs);
                    }
                    if (FD_ISSET(it->sd, &WriteFDs)) {
                        cout << "Socket " << it->sd << 
                                " became writable; handling it." <<
                                endl;
                        bOK = WriteData(*it);
                        pcErrorType = "Write error";
                        FD_CLR(it->sd, &WriteFDs);
                    }
                }

                if (!bOK) {
                    // Something bad happened on the socket, or the
                    // client closed its half of the connection.  Shut
                    // the conn down and remove it from the list.
                    int err;
                    int errlen = sizeof(err);
                    getsockopt(it->sd, SOL_SOCKET, SO_ERROR,
                            (char*)&err, &errlen);
                    if (err != NO_ERROR) {
                        cerr << WSAGetLastErrorMessage(pcErrorType,
                                err) << endl;
                    }
                    ShutdownConnection(it->sd);
                    gConnections.erase(it);
                    it = gConnections.begin();
                }
                else {
                    // Go on to next connection
                    ++it;
                }
            }
        }
        else {
            cerr << WSAGetLastErrorMessage("select() failed") << endl;
            return;
        }
    }
}

