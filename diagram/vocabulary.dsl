/*
 * This is a Design for Real-Time Vocabulary Quiz system
 *
*/
workspace "Vocabulary Quiz System" "This is an Real-Time Vocabulary Quiz system." {

    model {
        user = person "User" "A learner who uses this system to answer questions in real-time." "User"

        group "Vocabulary Quiz System" {

            vocabularyQuizSystem = softwaresystem "Vocabulary Quiz System" "Allows users to interact with the quiz, submit answers, and view the leaderboard." {

                singlePageApplication = container "Single-Page Application" "Provides quiz functionality to users via their web browser." "JavaScript and ReactJS" "Web Browser"
                mobileApp = container "Mobile App" "Allows users to interact with the quiz, submit answers, and view the leaderboard via their mobile device." "React Native" "Mobile App"
                
                identityApplication = container "Identity Server" "Handles user authentication and authorization." "Keycloak or a custom Python-based solution using OAuth2/OIDC" "Identity Provider" {
                    authController = component "Auth Controller" "Handles user authentication, authorization, and user management." "Python with Flask/Django"
                }
                /*identityApplication = container "Identity Server" "Handles user authentication and authorization." "Duende Identity Server" "Identity Provider" {
                    homeController = component "Home Controller" "Handles the default route to the SPA route ensuring users are signed in before doing any actions." "ASP.NET MVC Rest Controller"
                    signinController = component "Sign In Controller" "Allows users to sign in to the Vocabulary Quiz System." "ASP.NET MVC Rest Controller"
                    accountController = component "Accounts Controller" "Provides functionalities such as Reset Password, Forgot Password, etc." "ASP.NET MVC Rest Controller"
                    signoutController = component "Sign Out Controller" "Allows users to sign out of the Vocabulary Quiz System." "ASP.NET MVC Rest Controller"
                    registerController = component "Register Controller" "Provides functionalities related to user registration." "ASP.NET MVC Rest Controller"
                    errorController = component "Error Controller" "Handles errors when users interact with the Vocabulary Quiz System." "ASP.NET MVC Rest Controller" 
                }*/
                
                apiApplication = container "API Application" "Handles user's answers with Quiz system, displays scoring, and manages data persistence via a JSON/HTTPS API." "Python and Django" {
                    answersController = component "Answers Controller" "Handles answer submission and validation." "Python"
                    scoringController = component "Scoring Controller" "Calculates and retrieves scores from the database." "Python"
                    leaderboardController = component "Leaderboard Controller" "Manages and retrieves real-time leaderboard standings." "Python"
                }
                
                realTimeServer  = container "Real-Time Server" "Manages real-time communication between clients and the server." "Manages real-time for Web App" "Python with Django Channels"
                pushNotification = container "Push Notification Service" "Handles push notifications to mobile apps." "Firebase Cloud Messaging (FCM) for Android, Apple Push Notification Service (APNS) for iOS"
                
                
                backgroundWorker = container "Background Worker" "Processes quiz logic, score updates, and leaderboard updates." "Python with Celery"{
                    quizLogicProcessor = component "Quiz Logic Processor" "Handles the core quiz logic." "Python with Celery"
                    scoreUpdater = component "Score Updater" "Manages real-time score updates." "Python with Celery"
                    leaderboardUpdater = component "Leaderboard Updater" "Maintains and updates the real-time leaderboard." "Python with Celery"
                    notificationSender = component "Notification Sender" "Sends push notifications to users." "Python with Celery"
                    taskScheduler = component "Task Scheduler" "Schedules and manages periodic tasks." "Python with Celery Beat"
                }
                
                
                database = container "Database" "Stores quiz data, user information, scores, and leaderboard standings." "MongoDB" "Database"
                redis = container "Cache" "Handles real-time leaderboard updates and session management." "Redis" "Cache"
                messageQueue = container "Message Queue" "Handles communication between realTimeServer/Notification Hub and Background Worker." "RabbitMQ, Redis"
            }
        }
    

        # relationships between people and software systems
        user -> vocabularyQuizSystem "Views Leaderboard, scoring, and answer the quiz"        

        # relationships to/from containers
        user -> identityApplication "Login to the system" "HTTPS"
        user -> singlePageApplication "Views Leaderboard, scoring, and makes the answer for the quiz"
        user -> mobileApp "Views Leaderboard, scoring, and makes the answer for the quiz"

        apiApplication -> singlePageApplication "Delivers to the user's web browser"
        apiApplication -> messageQueue "Sends a request to background worker to update score or leaderboard"
        apiApplication -> realTimeServer "Trigger the new changes"

        backgroundWorker -> messageQueue "Retrieve the request and process the request parallel"
        backgroundWorker -> pushNotification "Push the content notification"
        pushNotification -> mobileApp "Boardcast message to the device"
        
        realTimeServer -> singlePageApplication "Inform client to know there is changes from server side, should request the new updated data"

        apiApplication -> redis "Retrieve or Save the data to the Redis"
        identityApplication -> redis "Retrieve or Save the token or short user to the Redis"

        identityApplication -> database "Retrieve or Save user info to the database"
        apiApplication -> database "Retrieve or Save the data"
        backgroundWorker -> database "Retrieve or Save the data"

        # relationships to/from components
        singlePageApplication -> answersController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> scoringController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> leaderboardController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> answersController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> scoringController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> leaderboardController "Makes API calls to" "JSON/HTTPS"
        
        signinController -> securityComponent "Uses"
        accountsSummaryController -> mainframeBankingSystemFacade "Uses"
        resetPasswordController -> securityComponent "Uses"
        resetPasswordController -> emailComponent "Uses"
        securityComponent -> database "Reads from and writes to" "SQL/TCP"
        mainframeBankingSystemFacade -> mainframe "Makes API calls to" "XML/HTTPS"
        emailComponent -> email "Sends e-mail using"

        deploymentEnvironment "Development" {
            deploymentNode "Developer Laptop" "" "Microsoft Windows 10 or Apple macOS" {
                deploymentNode "Web Browser" "" "Chrome, Firefox, Safari, or Edge" {
                    developerSinglePageApplicationInstance = containerInstance singlePageApplication
                }
                deploymentNode "Docker Container - Web Server" "" "Docker" {
                    deploymentNode "Apache Tomcat" "" "Apache Tomcat 8.x" {
                        developerWebApplicationInstance = containerInstance webApplication
                        developerApiApplicationInstance = containerInstance apiApplication
                    }
                }
                deploymentNode "Docker Container - Database Server" "" "Docker" {
                    deploymentNode "Database Server" "" "Oracle 12c" {
                        developerDatabaseInstance = containerInstance database
                    }
                }
            }
            deploymentNode "Big Bank plc" "" "Big Bank plc data center" "" {
                deploymentNode "bigbank-dev001" "" "" "" {
                    softwareSystemInstance mainframe
                }
            }

        }

        deploymentEnvironment "Live" {
            deploymentNode "Customer's mobile device" "" "Apple iOS or Android" {
                liveMobileAppInstance = containerInstance mobileApp
            }
            deploymentNode "Customer's computer" "" "Microsoft Windows or Apple macOS" {
                deploymentNode "Web Browser" "" "Chrome, Firefox, Safari, or Edge" {
                    liveSinglePageApplicationInstance = containerInstance singlePageApplication
                }
            }

            deploymentNode "Big Bank plc" "" "Big Bank plc data center" {
                deploymentNode "bigbank-web***" "" "Ubuntu 16.04 LTS" "" 4 {
                    deploymentNode "Apache Tomcat" "" "Apache Tomcat 8.x" {
                        liveWebApplicationInstance = containerInstance webApplication
                    }
                }
                deploymentNode "bigbank-api***" "" "Ubuntu 16.04 LTS" "" 8 {
                    deploymentNode "Apache Tomcat" "" "Apache Tomcat 8.x" {
                        liveApiApplicationInstance = containerInstance apiApplication
                    }
                }

                deploymentNode "bigbank-db01" "" "Ubuntu 16.04 LTS" {
                    primaryDatabaseServer = deploymentNode "Oracle - Primary" "" "Oracle 12c" {
                        livePrimaryDatabaseInstance = containerInstance database
                    }
                }
                deploymentNode "bigbank-db02" "" "Ubuntu 16.04 LTS" "Failover" {
                    secondaryDatabaseServer = deploymentNode "Oracle - Secondary" "" "Oracle 12c" "Failover" {
                        liveSecondaryDatabaseInstance = containerInstance database "Failover"
                    }
                }
                deploymentNode "bigbank-prod001" "" "" "" {
                    softwareSystemInstance mainframe
                }
            }

            primaryDatabaseServer -> secondaryDatabaseServer "Replicates data to"
        }
    }

    views {
        systemlandscape "SystemLandscape" {
            include *
            autoLayout
        }

        systemcontext internetBankingSystem "SystemContext" {
            include *
            animation {
                internetBankingSystem
                customer
                mainframe
                email
            }
            autoLayout
            description "The system context diagram for the Internet Banking System."
            properties {
                structurizr.groups false
            }
        }

        container internetBankingSystem "Containers" {
            include *
            animation {
                customer mainframe email
                webApplication
                singlePageApplication
                mobileApp
                apiApplication
                database
            }
            autoLayout
            description "The container diagram for the Internet Banking System."
        }

        component apiApplication "Components" {
            include *
            animation {
                singlePageApplication mobileApp database email mainframe
                signinController securityComponent
                accountsSummaryController mainframeBankingSystemFacade
                resetPasswordController emailComponent
            }
            autoLayout
            description "The component diagram for the API Application."
        }

        image mainframeBankingSystemFacade "MainframeBankingSystemFacade" {
            image https://raw.githubusercontent.com/structurizr/examples/main/dsl/big-bank-plc/internet-banking-system/mainframe-banking-system-facade.png
            title "[Code] Mainframe Banking System Facade"
        }

        dynamic apiApplication "SignIn" "Summarises how the sign in feature works in the single-page application." {
            singlePageApplication -> signinController "Submits credentials to"
            signinController -> securityComponent "Validates credentials using"
            securityComponent -> database "select * from users where username = ?"
            database -> securityComponent "Returns user data to"
            securityComponent -> signinController "Returns true if the hashed password matches"
            signinController -> singlePageApplication "Sends back an authentication token to"
            autoLayout
            description "Summarises how the sign in feature works in the single-page application."
        }

        deployment internetBankingSystem "Development" "DevelopmentDeployment" {
            include *
            animation {
                developerSinglePageApplicationInstance
                developerWebApplicationInstance developerApiApplicationInstance
                developerDatabaseInstance
            }
            autoLayout
            description "An example development deployment scenario for the Internet Banking System."
        }

        deployment internetBankingSystem "Live" "LiveDeployment" {
            include *
            animation {
                liveSinglePageApplicationInstance
                liveMobileAppInstance
                liveWebApplicationInstance liveApiApplicationInstance
                livePrimaryDatabaseInstance
                liveSecondaryDatabaseInstance
            }
            autoLayout
            description "An example live deployment scenario for the Internet Banking System."
        }

        styles {
            element "Person" {
                color #ffffff
                fontSize 22
                shape Person
            }
            element "Customer" {
                background #08427b
            }
            element "Bank Staff" {
                background #999999
            }
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "Existing System" {
                background #999999
                color #ffffff
            }
            element "Container" {
                background #438dd5
                color #ffffff
            }
            element "Web Browser" {
                shape WebBrowser
            }
            element "Mobile App" {
                shape MobileDeviceLandscape
            }
            element "Database" {
                shape Cylinder
            }
            element "Component" {
                background #85bbf0
                color #000000
            }
            element "Failover" {
                opacity 25
            }
        }
    }
}