pipeline {
    agent any

    environment {
        BRANCH = 'main'
    }

    stages {
        stage('Pull Lastest Code') {
            steps {
                script {
                    dir('/home/ubuntu/leeterview/leeterview-backend') {
                        withCredentials([sshUserPrivateKey(credentialsId: 'git-ssh-credentials', keyFileVariable: 'SSH_KEY')]) {
                            sh '''
                                eval $(ssh-agent -s)
                                ssh-add ${SSH_KEY}
                                git reset --hard
                                git fetch origin ${BRANCH}
                                git checkout ${BRANCH}
                                git pull origin ${BRANCH}
                            '''
                        }

                        def commitHash = sh(script: 'git rev-parse HEAD', returnStdout: true).trim()
                        echo "Current Git commit hash: ${commitHash}"
                        env.GIT_COMMIT_HASH = commitHash
                    }
                }
            }
        }

        stage('Stop and Remove Old Container') {
            steps {
                script {
                    dir('/home/ubuntu/leeterview') {

                        sh '''
                            docker-compose stop backend
                            docker-compose rm -f backend
                        '''
                    }
                }
            }
        }

        stage('Build and Run with Docker Compose') {
            steps {
                script {
                    dir('/home/ubuntu/leeterview') {
                        sh '''
                            export COMMIT_HASH=${GIT_COMMIT_HASH}
                            docker-compose build --no-cache backend
                            docker-compose up -d backend
                        '''
                    }
                }
            }
        }

        stage('Cleanup Old Docker Images') {
            steps {
                script {
                    sh '''
                        docker images leeterview-backend --format "{{.ID}} {{.CreatedAt}}" | \
                        tail -n +11 | awk '{print $1}' | xargs -r docker rmi -f
                    '''
                }
            }
        }

        stage('Show Commit Info and Update Jira') {
            steps {
                script {
                    def commitMsg = sh(script: 'git log -1 --pretty=%B', returnStdout: true).trim()
                    echo "Commit message: ${commitMsg}"

                    def jiraIssue = commitMsg =~ /LEET-\d+/
                    if (jiraIssue) {
                        def issueId = jiraIssue[0]
                        echo "Found Jira Issue: ${issueId}"

                        withCredentials([usernamePassword(credentialsId: 'ee7456d4-e6d3-43de-bbf2-9f54a35dcf76', usernameVariable: 'JIRA_USER', passwordVariable: 'JIRA_API_TOKEN')]) {
                            def transitionsResponse = sh(script: """
                                curl -u ${JIRA_USER}:${JIRA_API_TOKEN} \
                                    -X GET \
                                    -H "Content-Type: application/json" \
                                    https://newlife70723.atlassian.net/rest/api/3/issue/${issueId}/transitions
                            """, returnStdout: true).trim()

                            echo "Transitions Response: ${transitionsResponse}"

                            def jsonResponse = new groovy.json.JsonSlurper().parseText(transitionsResponse)
                            def transitionId = jsonResponse.transitions.find { it.name == '完成' }?.id

                            echo "Transition ID for 'Completed': ${transitionId}"

                            if (transitionId) {
                                def updateResponse = sh(script: """
                                    curl -u ${JIRA_USER}:${JIRA_API_TOKEN} \
                                        -X POST \
                                        -H "Content-Type: application/json" \
                                        -d '{
                                            "transition": {
                                                "id": "${transitionId}"
                                            }
                                        }' \
                                        https://newlife70723.atlassian.net/rest/api/3/issue/${issueId}/transitions
                                """, returnStdout: true).trim()

                                echo "Update Response: ${updateResponse}"
                            } else {
                                echo "Transition ID for 'Completed' not found."
                            }
                        }
                    } else {
                        echo "No Jira issue found in commit message."
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Pipeline executed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
        }
    }
}