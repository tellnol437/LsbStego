# LsbStego
LsbStego is an interactive C# console application for Windows. By using LsbStego, arbitrary files (called messages) can be hidden inside the Least Significant Bits of BMP or PNG images (called carriers). Thereby, a pre-shared stego password may be provided in order to prevent the message's extraction by unintended instances. Additionally, the tool may encrypt the messages thenselves using a pre-shared key and AES256-CBC before they are hidden.<br />
<br />
As a matter of fact, hiding a message inside the LSBs of any carrier image does not have a visually perceivable effect on the resulting stego image. Still, statistical attacks such as the Chi<sup>2</sup>-attack may detect the fact that a message has been embedded into the image. Therefore, LsbStego rates images according to their suitability as carriers for a specific message that is intended to be hidden. Once an image achieves a rating of approximately 90% or higher, the resulting stego image is immune against such statistical attacks.

## Usage
LsbStego is available as .exe file. The fact that it is interactive means that it leads a user through the whole embedding process. Once started, the entry point is the main menu which provides the options to hide or to extract a message. By entering "1" or "2", either the hiding or the extraction routine is invoked. All other input will not be validated.

### Hiding routine
LsbStego hides a message using Sequential Hide & Seek or a variant of Randomized Hide & Seek whereby the selection of message and carrier is done by the user. However, LsbStego supports the carrier’s selection by providing a suitability rating for each image regarding a specific message.

#### Providing the message
At the beginning of the hiding routine, the tool asks the user to provide a message. This is done by entering a relative path such as for example `Messages\TheMessage.pdf` in case the folder "Messages" is located in LsbStego’s root folder. Otherwise, if the message is stored somewhere else, an absolute path like `C:\Users\<userName>\Desktop\Messages\TheMessage.pdf` might be used. At this, the message can be an arbitrary file hence the file format or extension does not matter.

#### Encrypting the message
After providing the message path, the tool reads the message file and asks the user whether it is desired to encrypt the message before it is hidden. Therefore, the symmetric Rijndael cipher specified in the Advanced Encryption Standard with a block size of 128 bits and a key size of 256 bits is used. If the encryption is desired, the tool asks for a key which has to be entered twice, as shown in figure 64. This is to make sure no spelling mistakes occur. If no encryption is desired, the tool continues with the next step.

#### Securing the hiding routine
Before specifying a carrier image, LsbStego asks the user whether he wants to secure the hiding process by using a password or not. The use of such a stego password ensures that the message cannot be extracted by unintended instances and prevents visual attacks. As with the AES key, if one is desired, LsbStego forces the user to enter the password twice. Otherwise, the tool continues with the next step.

#### Selecting the carrier image
The next step is the selection of a carrier inside which the message is intended to be hidden. But instead of selecting a single carrier image, LsbStego asks the user to provide a whole directory storing at least one but rather multiple images. Again, this is done by entering a relative or absolute path. Afterwards, LsbStego searches the directory for any .bmp and .png files. All of these files are then given two ratings according to the Hamming distance of the message’s bitstring and the bitstring of all carrier image’s LSBs. This Hamming distance corresponds to the number of carrier bits that would have to be changed to embed the message.
- Rating r<sub>l</sub>: The amount of LSB changes related to the size of the message hence the percentage of message bits that already store the correct digit.
- Rating r<sub>c</sub>: The amount of LSB changes related to the carrier image’s capacity hence the percentage of carrier LSBs that already store the correct digit.
After scanning all potential carrier images, the user is forced to select one of the usable images to embed the message. The usable ones are all images with a capacity being greater or equal to the size of the message. All unusable carriers are marked with "--" in their ratings and are not selectable.

#### Hiding the message
After the carrier has been selected, the hiding process starts. Thereafter, the resulting stego image is written to the root folder of the LsbStego application and the user is returned to the main menu


### Extracting routine
Like other steganographic tools, LsbStego is only able to extract messages which have also been hidden with it. Furthermore, the proper extraction of a message is only possible if both the crypto key as well as the stego password are known. Otherwise, either the extraction will fail or a faulty file will be extracted.

#### Providing the stego image
At the beginning of the extraction routine, the tool asks the user to provide a stego image. Here again, this is done by entering a relative or absolute path.

#### Extracting the message
After the message has been read, LsbStego asks whether a password has been used to secure the hiding routine or not. This is done for security reasons because otherwise – if the tool would detect it on its own – it would be easy for an attacker to find out whether a password has been used or not by supplying the stego image to the LsbStego tool. However, it would be less secure to provide this information to any potential attackers. In case a password has been used, it needs to be entered to extract the message properly and if not, LsStego directly begins to extract the message. Nevertheless, if a password has been used to hide the message, but none is supplied to the extraction routine, completely different data will be extracted from the carrier image resulting in either a failure at the subsequent decryption operation or a wrong extracted file. The same applies if the supplied
stego password is wrong.

#### Decrypting the message
After the extraction of the message, LsbStego asks whether it has been encrypted for the same reasons it asks whether the hiding routine has been password secured. If it has been encrypted, the key needs to be supplied in order to decrypt the extracted message; if not, this process is skipped.

#### Writing the message
After the extraction of the message and its subsequent decryption, the message is written to the root folder of the LsbStego tool with the same file name it has been hidden with.


## Private Comment
I developed this tool for my master's thesis about "Image-based Steganography" in a 'quick and dirty' way under no license. There was no time wasted for efficiency or user friendliness.
