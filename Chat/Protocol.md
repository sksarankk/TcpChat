# chat protocol specification
server listens on 5000

## length prefixing

Every message starts with a 4-byte unsigned big-endian length prefix. Length of the message excluding length prefix.

## message type

the first 4 bytes of every message is the message type, as 4-byte big-endian unsigned integer

## message definition

### Type 0 : Chat message

The payload of the message is a UTF-8 encoded string.

