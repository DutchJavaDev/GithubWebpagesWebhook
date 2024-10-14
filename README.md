After creating https://github.com/DutchJavaDev/WebPageCreatorPOC I want to automate it using this project.

This will be a functions app running in azure that will act as a webhook (secured by a admin secret to prevent spam calls) that will be called/triggered when I update on of my repo's to update my github webpage for me
https://dutchjavadev.github.io/

Reason? I am lazy and also it gives a view of wat I am working on and wat I have worked on (handy for recruiters😜 who want to recruit me)

If you want to run this yourself you will need to configure the following enviroment variables for your function app

"GithubAccessToken" Generate an access token that has full access to repositories

"WebPagesRepositorieName" Name of your github pages repository example: DutchJavaDev.github.io

IMPORTANT: when deployed it will read the index.html file inside PageGenerator folder in the project folder, this needs to be copied to a blob container named "html-templates" inside the storage account that comes with the functions app

This is still a project under development.
