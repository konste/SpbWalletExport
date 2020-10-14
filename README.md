# SpbWalletExport

This is helper tool to allow export of data from SBP Wallet password manager as XML file to allow subsequent import of that data to other password managers.

* * * * *

**Project Description**

This is helper tool to allow export of data from SBP Wallet password manager as XML file to allow subsequent import of that data to other password managers.
This is to help to all users of SPB Wallet password manager to migrate their data to some other password manager of their choice.
SPB Wallet is not too bad. It is still for sale on SBP web site and many people still use it as of today.
But there a a few problems with it:
1. It is clearly abandoned project - no updates since 2010;
2. It is not as secure as many other password managers currently on the market;
3. Built-in export functionality produces plain text file, which is close to impossible to use as a source for import to other password managers;
4. It allows storing binary attachments to the cards, but those binary attachments are not even mentioned in its built-in export;
5. Intended mechanism for wallet synchronization between machines and devices is Gmail. Although it occasionally works, it is broken 90% of the time I have tried it.
The goal of my SPBWalletExport tool is to take SPB Wallet file (extension .swl) and produce fully decrypted XML file out of it. That XML file can be "massaged" to import it to many better password managers currently available.
Regarding file attachments. For every wallet card which has an attachment(s) I can provide file name of attachment, but not the file itself. I was able to decrypt attachments, but besides encryption they are also compressed with something like zlib and I simply had no time to figure how to unzip it properly. On the other hand people usually don't store too many attachments in their wallets, so as long as they have full list of cards with attachments it should be easy for the wallet owner to export those using SPB Wallet itself.
Specifically for **KeePass 2** [dhach](https://www.codeplex.com/site/users/view/dhach) authored [Python script](https://www.codeplex.com/site/users/view/dhach) to to convert XML output of this project to XML Import format of KeePass 2.

* * * * *
**How to use**

1. Build it yourself OR download the archive from Releases section and unzip it.
2. Edit *SpbWalletExport.exe.config* to enter the path to .swl file and your SpbWallet password.
3. Run *SpbWalletExport.exe*
