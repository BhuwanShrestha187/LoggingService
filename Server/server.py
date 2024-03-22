import asyncio
import os
import datetime
import time
import socket
import aiofiles
import threading
import yaml




TOTAL_MESSAGE_ELEMENTS = 4
CONFIG_FILE_NAME = "config.yaml"

#Global dictionary to track the client requests
client_requests = {}

#Rate limiting parameters
MAX_REQUESTS = 100 #Max requests per time window
TIME_WINDOW = datetime.timedelta(minutes=1) #Time window for rate limiting

def load_config_file():
    
    global IPADDRESS, PORT, MESSAGE_DELIMETER, BUFFER_SIZE, LOG_FILENAME
    global LOG_DIRECTORY, TIME_FORMAT, LOG_FORMAT, LOG_LEVELS
    global INDEX_OF_TIME, INDEX_OF_CLIENT_ID, INDEX_OF_LOG_LEVEL, INDEX_OF_MESSAGE
    
    
    #load from yaml config file in a string at first
    with open(CONFIG_FILE_NAME, 'r') as file:
        configData = yaml.safe_load(file)
    
    #Load the server_settings now
    server = configData["server_settings"]
    IPADDRESS = server.get("ipAddress")
    PORT = server.get("port")
    MESSAGE_DELIMETER = server.get("message_delimiter")
    BUFFER_SIZE = server.get("bufferSize")
    
    #Load the indexes of the log format
    INDEX_OF_TIME = configData["time_index"]
    INDEX_OF_CLIENT_ID = configData["client_id_index"]
    INDEX_OF_LOG_LEVEL = configData["log_level_index"]
    INDEX_OF_MESSAGE = configData["message_index"]
    
    #Load the log file settings
    logFileSettings = configData["log_file_settings"]
    LOG_FILENAME = logFileSettings.get("filename")
    LOG_DIRECTORY = logFileSettings.get("logDirectory")
    TIME_FORMAT = logFileSettings.get("log_time_format")
    LOG_FORMAT = logFileSettings.get("log_format")
    LOG_LEVELS = logFileSettings.get("log_levels")
    
  
def start_server(host, port):
    #Start the server and listens for connection
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((host, port))
    server_socket.listen(1)
    print(f"Server listening on port: {port}...")
    print("Waiting for the clients....")
    
    while True:
        client_socket, addr = server_socket.accept()
        client_thread = threading.Thread(target=handle_client, args=(client_socket, addr))  
        client_thread.start()
        print(f"Active Connections: {threading.active_count() - 1}")

#Handling if the message is in the right format
def is_message_format_valid(message, delimeter, expected_parts):
    """Check if the message contains the expected number of parts!!"""
    parts = message.split(delimeter)
    return len(parts) == expected_parts

def handle_incoming_message(message, addr):
    """Process incoming messages, handling misformatted messages as needeed"""
    if not is_message_format_valid(message, MESSAGE_DELIMETER, 4):
        #Handle misformatted message (e.g. log a warning, send an error response)
        print(f"Received a misformatted message: {message}")
        
    else:
        splitted_messages = message.split(MESSAGE_DELIMETER)
        process_log_message(splitted_messages, addr)
       

def handle_client(client_socket, addr):
    clientData = client_socket.recv(BUFFER_SIZE)
    actualData = clientData.decode()
    
    #Now check if the expected format of message is received or not
    handle_incoming_message(actualData, addr)   
    client_socket.close()
    
    
async def write_log(message):
    async with aiofiles.open(os.path.join(LOG_DIRECTORY, LOG_FILENAME), mode='a') as log_file:
        await log_file.write(message + '\n')
        

#Validate if the client ID is not empty
def is_valid_client_id(client_id):
    #Check if the client ID is not empty
    return bool(client_id.strip())

#Validate if the timestamp matches our expected format
def is_valid_timestamp(timestamp_str):
    timestamp_str = timestamp_str.strip()
    try:
        datetime.datetime.strptime(timestamp_str, TIME_FORMAT)
        return True
    except ValueError:
        return False
    
#Validate Log Level
def is_valid_log_level(log_level):
    #In here I had the problem at first, All are being stored in the log files, but WARNING is saying Invalid Log level
    # So, removing the leading and trailing characters before comparing solved the issue
    stripped_log_level = log_level.strip()
    return stripped_log_level in LOG_LEVELS.values()

#Validate MEssage
def is_valid_message(message):
    return bool(message.strip())

  
def process_log_message(splitted_messages, addr):
    #Now implement the validation first
    timestamp_str, client_id, log_level, message = splitted_messages
    #Perform Validation
    if not is_valid_client_id(client_id):
        print(f"Invalid Client ID received!!")
        return
    if not is_valid_timestamp(timestamp_str):
        print(f"Invalid timestamp: {timestamp_str}")
        return
    if not is_valid_log_level(log_level):
        print(f"Invalid log level: {log_level}")
        return
    if not is_valid_message(message):
        print(f"Invalid message: {message}")
        return
    
    if check_rate_limit(client_id):
        log_time = datetime.datetime.now().strftime(TIME_FORMAT)
        log_message = LOG_FORMAT.format(time=log_time, id=client_id, log_level=log_level, message=message)
        asyncio.run(write_log(log_message))
    else:
        # Rate limit exceeded; drop the message and log the violation
        violation_message = f"Rate limit exceeded for client {client_id} from {addr} at {datetime.datetime.now()}"
        print(violation_message)
        asyncio.run(write_log(violation_message))
    
    
    
def check_rate_limit(client_id):
    """
        Check if a client has exceeded the rate limit.
        Return true if the client is within the limit otherwise false.
    """
    
    current_time = datetime.datetime.now()
    if client_id not in client_requests:
        #If the client is not tracked yet, add them
        client_requests[client_id] = {'count' : 1, 'start_time':current_time}
        return True
    else:
        data = client_requests[client_id]
        elapsed_time = current_time - data['start_time']
        
        if elapsed_time > TIME_WINDOW:
            #If the current time window has passeed, reset the count and start time
            data['count'] = 1
            data['start_time'] = current_time
            return True
        else:
            if data['count'] < MAX_REQUESTS:
                #If within the rate limit, increment the count and allow the request
                data['count'] += 1
                return True
            else:
                #If rate limit is exceeded, deny the request
                return False
    
def main():
    #1. Load the config file
    load_config_file()
    
    #Check if the directory is there or not
    if not os.path.exists(LOG_DIRECTORY):
        os.makedirs(LOG_DIRECTORY)
        
    #Check if the file is already there or not, else make it
    if not os.path.exists(LOG_DIRECTORY + LOG_FILENAME):
        open(LOG_DIRECTORY + LOG_FILENAME, "w").close()
        
    #Create and start the socket now: 
    start_server(IPADDRESS, PORT)
        
    

if __name__ == "__main__":
    main()